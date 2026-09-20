using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using KassaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Controllers;

// "Mənə borclar" - mən müştəriyə borc verirəm
[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CashBoxService _cashBox;

    public LoansController(AppDbContext db, CashBoxService cashBox)
    {
        _db = db;
        _cashBox = cashBox;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LoanDto>>> GetAll(
        [FromQuery] string? currency, [FromQuery] string? clientName, [FromQuery] bool includeClosed = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Loans.Include(l => l.Client).Include(l => l.Payments).AsQueryable();

        if (!includeClosed)
            query = query.Where(l => l.Status != LoanStatus.Closed);

        if (!string.IsNullOrWhiteSpace(currency) && Enum.TryParse<Currency>(currency, true, out var cur))
            query = query.Where(l => l.Currency == cur);

        if (!string.IsNullOrWhiteSpace(clientName))
            query = query.Where(l => l.Client!.Name.Contains(clientName));

        query = query.OrderByDescending(l => l.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = items.Select(ToDto).ToList();

        return Ok(new PagedResult<LoanDto> { Items = dtos, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoanDto>> GetById(int id)
    {
        var loan = await _db.Loans.Include(l => l.Client).Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound();

        return Ok(ToDto(loan));
    }

    [HttpPost]
    public async Task<ActionResult<LoanDto>> Create(CreateLoanRequest request)
    {
        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
            return BadRequest(new { message = "Yanlış valyuta" });

        if (request.Amount <= 0)
            return BadRequest(new { message = "Məbləğ 0-dan böyük olmalıdır" });

        if (currency == Currency.USD && (request.ExchangeRate == null || request.ExchangeRate <= 0))
            return BadRequest(new { message = "Dollar üçün kurs qeyd olunmalıdır" });

        var client = await ClientsController.FindOrCreateAsync(_db, request.ClientName);

        var loan = new Loan
        {
            ClientId = client.Id,
            Currency = currency,
            Amount = request.Amount,
            ExchangeRate = currency == Currency.USD ? request.ExchangeRate : null,
            RemainingAmount = request.Amount,
            Status = LoanStatus.Open,
            Note = request.Note
        };

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync(); // Id lazımdır

        await _cashBox.ChangeBalanceAsync(currency, -request.Amount, CashSource.Loan, loan.Id,
            $"Borc verildi: {client.Name}");

        await _db.SaveChangesAsync();

        loan.Client = client;
        return Ok(ToDto(loan));
    }

    [HttpPost("{id}/payments")]
    public async Task<ActionResult<LoanDto>> AddPayment(int id, CreateLoanPaymentRequest request)
    {
        var loan = await _db.Loans.Include(l => l.Client).Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound();

        if (request.Amount <= 0)
            return BadRequest(new { message = "Məbləğ 0-dan böyük olmalıdır" });

        if (request.Amount > loan.RemainingAmount)
            return BadRequest(new { message = "Ödəniş qalan borcdan çox ola bilməz" });

        var payment = new LoanPayment { LoanId = loan.Id, Amount = request.Amount, Note = request.Note };
        _db.LoanPayments.Add(payment);

        loan.RemainingAmount -= request.Amount;
        loan.Status = loan.RemainingAmount <= 0 ? LoanStatus.Closed : LoanStatus.PartiallyPaid;

        await _cashBox.ChangeBalanceAsync(loan.Currency, request.Amount, CashSource.LoanPayment, loan.Id,
            $"Borc qaytarıldı: {loan.Client!.Name}");

        await _db.SaveChangesAsync();

        loan.Payments.Add(payment);
        return Ok(ToDto(loan));
    }

    private static LoanDto ToDto(Loan l) => new()
    {
        Id = l.Id,
        ClientName = l.Client?.Name ?? "",
        Currency = l.Currency.ToString(),
        Amount = l.Amount,
        ExchangeRate = l.ExchangeRate,
        RemainingAmount = l.RemainingAmount,
        Status = l.Status.ToString(),
        Note = l.Note,
        CreatedAt = l.CreatedAt,
        Payments = l.Payments.OrderByDescending(p => p.CreatedAt).Select(p => new LoanPaymentDto
        {
            Id = p.Id,
            Amount = p.Amount,
            Note = p.Note,
            CreatedAt = p.CreatedAt
        }).ToList()
    };

    [HttpPut("{id}")]
    public async Task<ActionResult<LoanDto>> Update(int id, UpdateLoanRequest request)
    {
        var loan = await _db.Loans.Include(l => l.Client).Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound();

        if (request.Amount <= 0)
            return BadRequest(new { message = "Məbləğ 0-dan böyük olmalıdır" });

        if (loan.Currency == Currency.USD && (request.ExchangeRate == null || request.ExchangeRate <= 0))
            return BadRequest(new { message = "Dollar üçün kurs qeyd olunmalıdır" });

        var totalPaid = loan.Payments.Sum(p => p.Amount);
        if (request.Amount < totalPaid)
            return BadRequest(new { message = $"Yeni məbləğ artıq qaytarılmış {totalPaid} {loan.Currency}-dan az ola bilməz" });

        // köhnə təsiri geri al
        await _cashBox.ChangeBalanceAsync(loan.Currency, loan.Amount, CashSource.Loan, loan.Id,
            $"Borc redaktəsi (geri alma): {loan.Client!.Name}");

        loan.Amount = request.Amount;
        loan.ExchangeRate = loan.Currency == Currency.USD ? request.ExchangeRate : null;
        loan.Note = request.Note;
        loan.RemainingAmount = request.Amount - totalPaid;
        loan.Status = loan.RemainingAmount <= 0 ? LoanStatus.Closed
            : totalPaid > 0 ? LoanStatus.PartiallyPaid : LoanStatus.Open;

        // yeni təsiri tətbiq et
        await _cashBox.ChangeBalanceAsync(loan.Currency, -request.Amount, CashSource.Loan, loan.Id,
            $"Borc redaktəsi (yeni): {loan.Client!.Name}");

        await _db.SaveChangesAsync();
        return Ok(ToDto(loan));
    }
}

using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using KassaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Controllers;

// "Mənim borclarım" - mən başqasından borc götürürəm
[ApiController]
[Route("api/mydebts")]
[Authorize]
public class MyDebtsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CashBoxService _cashBox;

    public MyDebtsController(AppDbContext db, CashBoxService cashBox)
    {
        _db = db;
        _cashBox = cashBox;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MyDebtDto>>> GetAll(
        [FromQuery] string? currency, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.MyDebts.Include(d => d.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(currency) && Enum.TryParse<Currency>(currency, true, out var cur))
            query = query.Where(d => d.Currency == cur);

        query = query.OrderByDescending(d => d.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = items.Select(ToDto).ToList();

        return Ok(new PagedResult<MyDebtDto> { Items = dtos, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<MyDebtDto>> Create(CreateMyDebtRequest request)
    {
        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
            return BadRequest(new { message = "Yanlış valyuta" });

        if (request.Amount <= 0)
            return BadRequest(new { message = "Məbləğ 0-dan böyük olmalıdır" });

        if (currency == Currency.USD && (request.ExchangeRate == null || request.ExchangeRate <= 0))
            return BadRequest(new { message = "Dollar üçün kurs qeyd olunmalıdır" });

        var client = await ClientsController.FindOrCreateAsync(_db, request.ClientName);

        var debt = new MyDebt
        {
            ClientId = client.Id,
            Currency = currency,
            Amount = request.Amount,
            ExchangeRate = currency == Currency.USD ? request.ExchangeRate : null,
            Note = request.Note
        };

        _db.MyDebts.Add(debt);
        await _db.SaveChangesAsync();

        // Mən borc götürəndə pul mənim kassama daxil olur
        await _cashBox.ChangeBalanceAsync(currency, request.Amount, CashSource.MyDebt, debt.Id,
            $"Borc alındı: {client.Name}");

        await _db.SaveChangesAsync();

        debt.Client = client;
        return Ok(ToDto(debt));
    }

    private static MyDebtDto ToDto(MyDebt d) => new()
    {
        Id = d.Id,
        ClientName = d.Client?.Name ?? "",
        Currency = d.Currency.ToString(),
        Amount = d.Amount,
        ExchangeRate = d.ExchangeRate,
        Note = d.Note,
        CreatedAt = d.CreatedAt
    };

    [HttpPut("{id}")]
    public async Task<ActionResult<MyDebtDto>> Update(int id, UpdateMyDebtRequest request)
    {
        var debt = await _db.MyDebts.Include(d => d.Client).FirstOrDefaultAsync(d => d.Id == id);
        if (debt == null) return NotFound();

        if (request.Amount <= 0)
            return BadRequest(new { message = "Məbləğ 0-dan böyük olmalıdır" });

        if (debt.Currency == Currency.USD && (request.ExchangeRate == null || request.ExchangeRate <= 0))
            return BadRequest(new { message = "Dollar üçün kurs qeyd olunmalıdır" });

        await _cashBox.ChangeBalanceAsync(debt.Currency, -debt.Amount, CashSource.MyDebt, debt.Id,
            $"Borc redaktəsi (geri alma): {debt.Client!.Name}");

        debt.Amount = request.Amount;
        debt.ExchangeRate = debt.Currency == Currency.USD ? request.ExchangeRate : null;
        debt.Note = request.Note;

        await _cashBox.ChangeBalanceAsync(debt.Currency, request.Amount, CashSource.MyDebt, debt.Id,
            $"Borc redaktəsi (yeni): {debt.Client!.Name}");

        await _db.SaveChangesAsync();
        return Ok(ToDto(debt));
    }
}

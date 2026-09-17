using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using KassaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Controllers;

[ApiController]
[Route("api/exchange")]
[Authorize]
public class ExchangeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CashBoxService _cashBox;

    public ExchangeController(AppDbContext db, CashBoxService cashBox)
    {
        _db = db;
        _cashBox = cashBox;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ExchangeDto>>> GetAll(
        [FromQuery] string? fromCurrency, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var queryable = _db.Exchanges.Include(e => e.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(fromCurrency) && Enum.TryParse<Currency>(fromCurrency, true, out var fc))
            queryable = queryable.Where(e => e.FromCurrency == fc);

        var query = queryable.OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResult<ExchangeDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost]
    public async Task<ActionResult<ExchangeDto>> Create(CreateExchangeRequest request)
    {
        if (!Enum.TryParse<Currency>(request.FromCurrency, true, out var from) ||
            !Enum.TryParse<Currency>(request.ToCurrency, true, out var to) || from == to)
            return BadRequest(new { message = "Valyutalar fərqli olmalıdır (USD/RUB)" });

        if (request.FromAmount <= 0 || request.Rate <= 0)
            return BadRequest(new { message = "Məbləğ və kurs 0-dan böyük olmalıdır" });

        var client = await ClientsController.FindOrCreateAsync(_db, request.ClientName);
        var toAmount = CalcToAmount(from, to, request.FromAmount, request.Rate);

        var exchange = new Exchange
        {
            ClientId = client.Id,
            FromCurrency = from,
            ToCurrency = to,
            FromAmount = request.FromAmount,
            Rate = request.Rate,
            ToAmount = toAmount,
            Note = request.Note
        };

        _db.Exchanges.Add(exchange);
        await _db.SaveChangesAsync();

        // Client bizə "from" valyutasını verir -> bizim "from" kassamız artır
        await _cashBox.ChangeBalanceAsync(from, request.FromAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə ({from}->{to}): {client.Name}");
        // Biz ona "to" valyutasını veririk -> bizim "to" kassamız azalır
        await _cashBox.ChangeBalanceAsync(to, -toAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə ({from}->{to}): {client.Name}");

        await _db.SaveChangesAsync();

        exchange.Client = client;
        return Ok(ToDto(exchange));
    }

    // RUB -> USD zamanı məbləğ kursa bölünür, USD -> RUB zamanı isə vurulur
    private static decimal CalcToAmount(Currency from, Currency to, decimal fromAmount, decimal rate)
    {
        if (from == Currency.RUB && to == Currency.USD)
            return fromAmount / rate;
        return fromAmount * rate;
    }

    private static ExchangeDto ToDto(Exchange e) => new()
    {
        Id = e.Id,
        ClientName = e.Client?.Name ?? "",
        FromCurrency = e.FromCurrency.ToString(),
        ToCurrency = e.ToCurrency.ToString(),
        FromAmount = e.FromAmount,
        Rate = e.Rate,
        ToAmount = e.ToAmount,
        Note = e.Note,
        CreatedAt = e.CreatedAt
    };

    [HttpPut("{id}")]
    public async Task<ActionResult<ExchangeDto>> Update(int id, UpdateExchangeRequest request)
    {
        var exchange = await _db.Exchanges.Include(e => e.Client).FirstOrDefaultAsync(e => e.Id == id);
        if (exchange == null) return NotFound();

        if (request.FromAmount <= 0 || request.Rate <= 0)
            return BadRequest(new { message = "Məbləğ və kurs 0-dan böyük olmalıdır" });

        await _cashBox.ChangeBalanceAsync(exchange.FromCurrency, -exchange.FromAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (geri alma): {exchange.Client!.Name}");
        await _cashBox.ChangeBalanceAsync(exchange.ToCurrency, exchange.ToAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (geri alma): {exchange.Client!.Name}");

        exchange.FromAmount = request.FromAmount;
        exchange.Rate = request.Rate;
        exchange.ToAmount = CalcToAmount(exchange.FromCurrency, exchange.ToCurrency, request.FromAmount, request.Rate);
        exchange.Note = request.Note;

        await _cashBox.ChangeBalanceAsync(exchange.FromCurrency, request.FromAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (yeni): {exchange.Client!.Name}");
        await _cashBox.ChangeBalanceAsync(exchange.ToCurrency, -exchange.ToAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (yeni): {exchange.Client!.Name}");

        await _db.SaveChangesAsync();
        return Ok(ToDto(exchange));
    }
}

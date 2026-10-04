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
        [FromQuery] string? fromCurrency, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] bool current = false)
    {
        var queryable = _db.Exchanges.Include(e => e.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(fromCurrency) && Enum.TryParse<Currency>(fromCurrency, true, out var fc))
            queryable = queryable.Where(e => e.FromCurrency == fc);

        if (current)
        {
            var latestClose = await _db.DayCloses
                .OrderByDescending(d => d.ClosedAt)
                .Select(d => (DateTime?)d.ClosedAt)
                .FirstOrDefaultAsync();

            if (latestClose != null)
            {
                // "Günü bitir"-dən sonrakı əməliyyatlar + hələ bağlanmamış (tam satılmamış) alış partiyaları
                queryable = queryable.Where(e =>
                    e.CreatedAt > latestClose ||
                    (e.FromCurrency == Currency.USD && e.ToCurrency == Currency.RUB &&
                     _db.CurrencyLots.Any(l => l.SourceExchangeId == e.Id && l.RemainingAmount > 0)));
            }
        }

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

    // Bütün "Dollar satışı" qeydlərindən əldə edilmiş cəmi qazanc (bütün tarixçə üzrə)
    [HttpGet("profit-summary")]
    public async Task<ActionResult<ExchangeProfitSummaryDto>> GetProfitSummary()
    {
        var total = await _db.Exchanges
            .Where(e => e.RealizedProfit != null)
            .SumAsync(e => e.RealizedProfit ?? 0);

        return Ok(new ExchangeProfitSummaryDto { TotalRealizedProfit = total });
    }

    // Dollar partiyaları - ən köhnədən yeniyə (FIFO-nun işləyəcəyi sıra ilə).
    // includeClosed=false (default): yalnız açıq (hələ tam satılmamış) partiyalar.
    // includeClosed=true: bağlanmış (tam satılmış) partiyalar da daxil - partiyanın tam tarixçəsini görmək üçün.
    [HttpGet("lots")]
    public async Task<ActionResult<List<CurrencyLotDto>>> GetOpenLots([FromQuery] bool includeClosed = false)
    {
        var query = _db.CurrencyLots.Where(l => l.Currency == Currency.USD);
        if (!includeClosed)
            query = query.Where(l => l.RemainingAmount > 0);

        var lots = await query.OrderBy(l => l.CreatedAt).ToListAsync();

        return Ok(lots.Select(l => new CurrencyLotDto
        {
            Id = l.Id,
            Currency = l.Currency.ToString(),
            Rate = l.Rate,
            OriginalAmount = l.OriginalAmount,
            RemainingAmount = l.RemainingAmount,
            CreatedAt = l.CreatedAt
        }).ToList());
    }

    // Bir satışın hansı partiya(lar)dan, nə qədər və hansı qazancla qarşılandığının detalı ("qəbz")
    [HttpGet("{id}/consumptions")]
    public async Task<ActionResult<List<LotConsumptionDetailDto>>> GetConsumptions(int id)
    {
        var exchange = await _db.Exchanges.FindAsync(id);
        if (exchange == null) return NotFound();

        var consumptions = await _db.LotConsumptions
            .Include(c => c.Lot)
            .Where(c => c.SellExchangeId == id)
            .OrderBy(c => c.Lot!.CreatedAt)
            .ToListAsync();

        return Ok(consumptions.Select(c => new LotConsumptionDetailDto
        {
            LotId = c.LotId,
            LotRate = c.Rate,
            LotCreatedAt = c.Lot?.CreatedAt ?? default,
            Amount = c.Amount,
            Profit = c.Amount * (exchange.Rate - c.Rate)
        }).ToList());
    }

    // Bir partiyanın hansı satış(lar)a, nə qədər və hansı qazancla getdiyinin tarixçəsi
    [HttpGet("lots/{lotId}/sales")]
    public async Task<ActionResult<List<LotSaleDetailDto>>> GetLotSales(int lotId)
    {
        var consumptions = await _db.LotConsumptions
            .Include(c => c.SellExchange)
            .Where(c => c.LotId == lotId)
            .OrderBy(c => c.SellExchange!.CreatedAt)
            .ToListAsync();

        return Ok(consumptions.Select(c => new LotSaleDetailDto
        {
            SellExchangeId = c.SellExchangeId,
            SellCreatedAt = c.SellExchange?.CreatedAt ?? default,
            SellRate = c.SellExchange?.Rate ?? 0,
            Amount = c.Amount,
            Profit = c.Amount * ((c.SellExchange?.Rate ?? 0) - c.Rate)
        }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ExchangeDto>> Create(CreateExchangeRequest request)
    {
        if (!Enum.TryParse<Currency>(request.FromCurrency, true, out var from) ||
            !Enum.TryParse<Currency>(request.ToCurrency, true, out var to) || from == to)
            return BadRequest(new { message = "Valyutalar fərqli olmalıdır (USD/RUB)" });

        if (request.FromAmount <= 0 || request.Rate <= 0)
            return BadRequest(new { message = "Məbləğ və kurs 0-dan böyük olmalıdır" });

        var clientName = string.IsNullOrWhiteSpace(request.ClientName) ? "Naməlum müştəri" : request.ClientName;
        var client = await ClientsController.FindOrCreateAsync(_db, clientName);
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
        await _db.SaveChangesAsync(); // Id lazımdır (partiya/istehlak qeydləri üçün)

        // Client bizə "from" valyutasını verir -> bizim "from" kassamız artır
        await _cashBox.ChangeBalanceAsync(from, request.FromAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə ({from}->{to}): {client.Name}");
        // Biz ona "to" valyutasını veririk -> bizim "to" kassamız azalır
        await _cashBox.ChangeBalanceAsync(to, -toAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə ({from}->{to}): {client.Name}");

        // Dollar alışı/satışı maya dəyəri izlənməsi (FIFO)
        if (from == Currency.USD && to == Currency.RUB)
        {
            // Dollar alışı: yeni partiya yaradılır
            _db.CurrencyLots.Add(new CurrencyLot
            {
                Currency = Currency.USD,
                OriginalAmount = request.FromAmount,
                RemainingAmount = request.FromAmount,
                Rate = request.Rate,
                SourceExchangeId = exchange.Id
            });
        }
        else if (from == Currency.RUB && to == Currency.USD)
        {
            // Dollar satışı: mövcud partiyalardan FIFO ilə çıxılır, qazanc hesablanır
            exchange.RealizedProfit = await ConsumeLotsFifoAsync(toAmount, request.Rate, exchange.Id);
        }

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

    // USD partiyalarından FIFO (ən köhnədən) sırayla çıxılır, qazanc = çıxılan_miqdar * (satış_kursu - partiyanın_kursu).
    // Partiyalar kifayət etməsə (məs. USD borc kimi gəlmişdisə), qalan hissənin maya dəyəri bilinmir - qazanca daxil edilmir.
    private async Task<decimal> ConsumeLotsFifoAsync(decimal usdAmount, decimal sellRate, int sellExchangeId)
    {
        var remaining = usdAmount;
        decimal profit = 0;

        var lots = await _db.CurrencyLots
            .Where(l => l.Currency == Currency.USD && l.RemainingAmount > 0)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        foreach (var lot in lots)
        {
            if (remaining <= 0) break;

            var consume = Math.Min(remaining, lot.RemainingAmount);
            lot.RemainingAmount -= consume;
            remaining -= consume;
            profit += consume * (sellRate - lot.Rate);

            _db.LotConsumptions.Add(new LotConsumption
            {
                LotId = lot.Id,
                SellExchangeId = sellExchangeId,
                Amount = consume,
                Rate = lot.Rate
            });
        }

        return profit;
    }

    // Bir satışın əvvəlki partiya istehlaklarını geri qaytarır (redaktə zamanı yenidən hesablamaq üçün)
    private async Task ReverseLotConsumptionsAsync(int sellExchangeId)
    {
        var consumptions = await _db.LotConsumptions
            .Include(c => c.Lot)
            .Where(c => c.SellExchangeId == sellExchangeId)
            .ToListAsync();

        foreach (var c in consumptions)
        {
            if (c.Lot != null)
                c.Lot.RemainingAmount += c.Amount;

            _db.LotConsumptions.Remove(c); // ISoftDeletable -> soft-delete
        }
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
        RealizedProfit = e.RealizedProfit,
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

        // Alış (bu qeydin yaratdığı partiyadan artıq satılıb-satılmadığını yoxla)
        if (exchange.FromCurrency == Currency.USD && exchange.ToCurrency == Currency.RUB)
        {
            var lot = await _db.CurrencyLots.FirstOrDefaultAsync(l => l.SourceExchangeId == exchange.Id);
            if (lot != null)
            {
                var alreadyConsumed = lot.OriginalAmount - lot.RemainingAmount;
                if (request.FromAmount < alreadyConsumed)
                    return BadRequest(new { message = $"Bu partiyadan artıq {alreadyConsumed} USD satılıb, məbləği bundan az edə bilməzsiniz" });

                lot.OriginalAmount = request.FromAmount;
                lot.RemainingAmount = request.FromAmount - alreadyConsumed;
                lot.Rate = request.Rate;
            }
        }

        await _cashBox.ChangeBalanceAsync(exchange.FromCurrency, -exchange.FromAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (geri alma): {exchange.Client!.Name}");
        await _cashBox.ChangeBalanceAsync(exchange.ToCurrency, exchange.ToAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (geri alma): {exchange.Client!.Name}");

        exchange.FromAmount = request.FromAmount;
        exchange.Rate = request.Rate;
        exchange.ToAmount = CalcToAmount(exchange.FromCurrency, exchange.ToCurrency, request.FromAmount, request.Rate);
        exchange.Note = request.Note;

        // Satış (köhnə partiya istehlakını geri qaytarıb yeni məbləğ/kursla yenidən hesabla)
        if (exchange.FromCurrency == Currency.RUB && exchange.ToCurrency == Currency.USD)
        {
            await ReverseLotConsumptionsAsync(exchange.Id);
            exchange.RealizedProfit = await ConsumeLotsFifoAsync(exchange.ToAmount, exchange.Rate, exchange.Id);
        }

        await _cashBox.ChangeBalanceAsync(exchange.FromCurrency, request.FromAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (yeni): {exchange.Client!.Name}");
        await _cashBox.ChangeBalanceAsync(exchange.ToCurrency, -exchange.ToAmount, CashSource.Exchange, exchange.Id,
            $"Mübadilə redaktəsi (yeni): {exchange.Client!.Name}");

        await _db.SaveChangesAsync();
        return Ok(ToDto(exchange));
    }
}

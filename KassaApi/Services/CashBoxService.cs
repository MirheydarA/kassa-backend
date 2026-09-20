using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Services;

public class CashBoxService
{
    private readonly AppDbContext _db;

    public CashBoxService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Kassa balansını dəyişir (delta müsbət/mənfi ola bilər) və hərəkət tarixçəsinə yazır.
    /// SaveChanges çağırmır - eyni "unit of work" içində çağıran tərəf SaveChangesAsync etməlidir.
    /// </summary>
    public async Task ChangeBalanceAsync(Currency currency, decimal delta, CashSource source, int? referenceId, string description)
    {
        var balance = await _db.CashBoxBalances.FirstOrDefaultAsync(b => b.Currency == currency);
        if (balance == null)
        {
            balance = new CashBoxBalance { Currency = currency, Amount = 0 };
            _db.CashBoxBalances.Add(balance);
        }

        balance.Amount += delta;

        _db.CashBoxTransactions.Add(new CashBoxTransaction
        {
            Currency = currency,
            Type = delta >= 0 ? TransactionType.In : TransactionType.Out,
            Amount = Math.Abs(delta),
            Source = source,
            ReferenceId = referenceId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Bir kassa hərəkətini geri qaytarır: balansa əks təsiri tətbiq edir və hərəkəti (soft) silir.
    /// SaveChanges çağırmır - çağıran tərəf SaveChangesAsync etməlidir.
    /// </summary>
    public async Task<CashBoxTransaction?> RevertTransactionAsync(int transactionId)
    {
        var tx = await _db.CashBoxTransactions.FirstOrDefaultAsync(t => t.Id == transactionId);
        if (tx == null) return null;

        var balance = await _db.CashBoxBalances.FirstOrDefaultAsync(b => b.Currency == tx.Currency);
        if (balance == null)
        {
            balance = new CashBoxBalance { Currency = tx.Currency, Amount = 0 };
            _db.CashBoxBalances.Add(balance);
        }

        // Hərəkətin əks təsirini tətbiq et: "In" idisə çıx, "Out" idisə geri qoy
        balance.Amount += tx.Type == TransactionType.In ? -tx.Amount : tx.Amount;

        _db.CashBoxTransactions.Remove(tx); // ISoftDeletable -> avtomatik soft-delete olunur

        return tx;
    }

    public async Task<CashBoxBalanceDto> GetBalanceAsync()
    {
        var balances = await _db.CashBoxBalances.ToListAsync();
        return new CashBoxBalanceDto
        {
            Usd = balances.FirstOrDefault(b => b.Currency == Currency.USD)?.Amount ?? 0,
            Rub = balances.FirstOrDefault(b => b.Currency == Currency.RUB)?.Amount ?? 0
        };
    }

    // Frontend-in gördüyü "tip" (loan, loan_payment, mydebt, mydebt_payment, exchange_in, exchange_out, expense)
    // Source+Type-in kombinasiyasıdır - Exchange üçün Type (In/Out) da lazımdır, qalanlar üçün Source kifayətdir.
    private static string ComputeLogicalType(CashSource source, TransactionType type) => source switch
    {
        CashSource.Loan => "loan",
        CashSource.LoanPayment => "loan_payment",
        CashSource.MyDebt => "mydebt",
        CashSource.MyDebtPayment => "mydebt_payment",
        CashSource.Exchange => type == TransactionType.In ? "exchange_in" : "exchange_out",
        CashSource.Expense => "expense",
        CashSource.Adjustment => "adjustment",
        _ => source.ToString().ToLowerInvariant()
    };

    private static bool TryParseLogicalType(string value, out CashSource source, out TransactionType? type)
    {
        switch (value)
        {
            case "loan": source = CashSource.Loan; type = null; return true;
            case "loan_payment": source = CashSource.LoanPayment; type = null; return true;
            case "mydebt": source = CashSource.MyDebt; type = null; return true;
            case "mydebt_payment": source = CashSource.MyDebtPayment; type = null; return true;
            case "exchange_in": source = CashSource.Exchange; type = TransactionType.In; return true;
            case "exchange_out": source = CashSource.Exchange; type = TransactionType.Out; return true;
            case "expense": source = CashSource.Expense; type = null; return true;
            case "adjustment": source = CashSource.Adjustment; type = null; return true;
            default: source = default; type = null; return false;
        }
    }

    private static IQueryable<CashBoxTransaction> ApplyFilters(
        IQueryable<CashBoxTransaction> query, Currency? currency, string? logicalType, DateTime? from, DateTime? to)
    {
        if (currency.HasValue) query = query.Where(t => t.Currency == currency.Value);

        if (!string.IsNullOrWhiteSpace(logicalType) && TryParseLogicalType(logicalType, out var source, out var type))
        {
            query = query.Where(t => t.Source == source);
            if (type.HasValue) query = query.Where(t => t.Type == type.Value);
        }

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value.Date);
        // "to" tarixin bütün günü daxil olsun (yalnız gecə yarısına qədər deyil)
        if (to.HasValue) query = query.Where(t => t.CreatedAt < to.Value.Date.AddDays(1));

        return query;
    }

    private static CashBoxTransactionDto ToDto(CashBoxTransaction t) => new()
    {
        Id = t.Id,
        Currency = t.Currency.ToString(),
        Type = ComputeLogicalType(t.Source, t.Type),
        Amount = t.Amount,
        Source = t.Source.ToString(),
        Description = t.Description,
        CreatedAt = t.CreatedAt
    };

    public async Task<PagedResult<CashBoxTransactionDto>> GetTransactionsAsync(
        Currency? currency, string? logicalType, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = ApplyFilters(_db.CashBoxTransactions.AsQueryable(), currency, logicalType, from, to);
        query = query.OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();

        var entities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<CashBoxTransactionDto>
        {
            Items = entities.Select(ToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // Səhifələmə səhifə/ölçü ilə deyil, gün-gün aparılır: hər çağırış bir günün bütün hərəkətlərini qaytarır.
    // Beləliklə frontend heç vaxt böyük məlumat massivi yükləmir (böyük datasetlərdə performans üçün).
    public async Task<CashBoxDayResultDto> GetTransactionsByDayAsync(
        Currency? currency, string? logicalType, DateTime? from, DateTime? to, int dayPage)
    {
        var filtered = ApplyFilters(_db.CashBoxTransactions.AsQueryable(), currency, logicalType, from, to);

        var distinctDays = await filtered
            .Select(t => t.CreatedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        var totalDays = distinctDays.Count;
        if (totalDays == 0)
            return new CashBoxDayResultDto { Date = null, Items = new(), DayPage = 1, TotalDays = 0 };

        dayPage = Math.Clamp(dayPage, 1, totalDays);
        var targetDate = distinctDays[dayPage - 1];

        var dayEntities = await filtered
            .Where(t => t.CreatedAt >= targetDate && t.CreatedAt < targetDate.AddDays(1))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return new CashBoxDayResultDto
        {
            Date = targetDate,
            Items = dayEntities.Select(ToDto).ToList(),
            DayPage = dayPage,
            TotalDays = totalDays
        };
    }
}

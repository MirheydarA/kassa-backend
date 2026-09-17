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

    public async Task<PagedResult<CashBoxTransactionDto>> GetTransactionsAsync(
        Currency? currency, TransactionType? type, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.CashBoxTransactions.AsQueryable();

        if (currency.HasValue) query = query.Where(t => t.Currency == currency.Value);
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);
        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        query = query.OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new CashBoxTransactionDto
            {
                Id = t.Id,
                Currency = t.Currency.ToString(),
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Source = t.Source.ToString(),
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<CashBoxTransactionDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

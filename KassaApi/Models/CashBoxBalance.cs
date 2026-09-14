namespace KassaApi.Models;

public class CashBoxBalance : ISoftDeletable
{
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

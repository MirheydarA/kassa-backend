namespace KassaApi.Models;

public class CashBoxTransaction : ISoftDeletable
{
    public int Id { get; set; }
    public Currency Currency { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public CashSource Source { get; set; }
    public int? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

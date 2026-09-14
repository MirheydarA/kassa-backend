namespace KassaApi.Models;

// Valyuta mübadiləsi (Exchange)
public class Exchange : ISoftDeletable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public Currency FromCurrency { get; set; }
    public Currency ToCurrency { get; set; }
    public decimal FromAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal ToAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

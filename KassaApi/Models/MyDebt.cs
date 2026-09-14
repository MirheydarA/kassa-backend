namespace KassaApi.Models;

// Mənim başqasından götürdüyüm borc
public class MyDebt : ISoftDeletable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

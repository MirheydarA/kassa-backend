namespace KassaApi.Models;

// Mən müştəriyə borc verirəm
public class Loan : ISoftDeletable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; } // yalnız USD olduqda dolur
    public decimal RemainingAmount { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Open;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<LoanPayment> Payments { get; set; } = new();
}

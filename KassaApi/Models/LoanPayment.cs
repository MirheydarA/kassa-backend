namespace KassaApi.Models;

// Müştərinin borcunu (hissə-hissə) qaytarması
public class LoanPayment : ISoftDeletable
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public Loan? Loan { get; set; }

    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

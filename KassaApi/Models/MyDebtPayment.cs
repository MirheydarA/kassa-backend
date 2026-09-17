namespace KassaApi.Models;

// Mənim götürdüyüm borcu (hissə-hissə) qaytarmağım
public class MyDebtPayment : ISoftDeletable
{
    public int Id { get; set; }
    public int MyDebtId { get; set; }
    public MyDebt? MyDebt { get; set; }

    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

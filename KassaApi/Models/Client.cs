namespace KassaApi.Models;

public class Client : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<Loan> Loans { get; set; } = new();
    public List<MyDebt> MyDebts { get; set; } = new();
    public List<Exchange> Exchanges { get; set; } = new();
}

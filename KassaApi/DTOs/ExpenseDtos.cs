namespace KassaApi.DTOs;

public class CreateExpenseRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ExpenseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateExpenseRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
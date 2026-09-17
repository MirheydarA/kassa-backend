namespace KassaApi.DTOs;

public class CashBoxBalanceDto
{
    public decimal Usd { get; set; }
    public decimal Rub { get; set; }
}

public class CashBoxTransactionDto
{
    public int Id { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RevertTransactionRequest
{
    public string Password { get; set; } = string.Empty;
}

public class CashBoxDayResultDto
{
    public DateTime? Date { get; set; }
    public List<CashBoxTransactionDto> Items { get; set; } = new();
    public int DayPage { get; set; }
    public int TotalDays { get; set; }
}

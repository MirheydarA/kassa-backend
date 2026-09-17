namespace KassaApi.DTOs;

public class CreateMyDebtRequest
{
    public string ClientName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? Note { get; set; }
}

public class MyDebtDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MyDebtPaymentDto> Payments { get; set; } = new();
}

public class UpdateMyDebtRequest
{
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? Note { get; set; }
}

public class CreateMyDebtPaymentRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class MyDebtPaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

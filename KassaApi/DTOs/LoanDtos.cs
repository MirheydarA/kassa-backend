namespace KassaApi.DTOs;

public class CreateLoanRequest
{
    public string ClientName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD"; // "USD" | "RUB"
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? Note { get; set; }
}

public class CreateLoanPaymentRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class LoanDto
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
    public List<LoanPaymentDto> Payments { get; set; } = new();
}

public class LoanPaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class UpdateLoanRequest
{
    public decimal Amount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? Note { get; set; }
}
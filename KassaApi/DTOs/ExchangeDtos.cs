namespace KassaApi.DTOs;

public class CreateExchangeRequest
{
    public string ClientName { get; set; } = string.Empty;
    public string FromCurrency { get; set; } = "USD";
    public string ToCurrency { get; set; } = "RUB";
    public decimal FromAmount { get; set; }
    public decimal Rate { get; set; }
    public string? Note { get; set; }
}

public class ExchangeDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal FromAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal ToAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateExchangeRequest
{
    public decimal FromAmount { get; set; }
    public decimal Rate { get; set; }
    public string? Note { get; set; }
}
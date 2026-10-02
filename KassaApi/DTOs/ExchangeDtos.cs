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
    public decimal? RealizedProfit { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateExchangeRequest
{
    public decimal FromAmount { get; set; }
    public decimal Rate { get; set; }
    public string? Note { get; set; }
}

public class ExchangeProfitSummaryDto
{
    public decimal TotalRealizedProfit { get; set; }
}

public class CurrencyLotDto
{
    public int Id { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Bir satışın hansı partiya(lar)dan, nə qədər və hansı qazancla qarşılandığını göstərir ("qəbz" detalı)
public class LotConsumptionDetailDto
{
    public int LotId { get; set; }
    public decimal LotRate { get; set; }
    public DateTime LotCreatedAt { get; set; }
    public decimal Amount { get; set; }
    public decimal Profit { get; set; }
}

// Bir partiyanın hansı satış(lar)a, nə qədər və hansı qazancla getdiyini göstərir (partiyanın "tale"si)
public class LotSaleDetailDto
{
    public int SellExchangeId { get; set; }
    public DateTime SellCreatedAt { get; set; }
    public decimal SellRate { get; set; }
    public decimal Amount { get; set; }
    public decimal Profit { get; set; }
}
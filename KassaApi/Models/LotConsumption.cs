namespace KassaApi.Models;

// Bir "Dollar satışı" Exchange qeydinin hansı partiyadan nə qədər çıxdığının qeydi.
// Rate - partiyanın (alışın) o andakı kursu (qazanc hesabında istifadə olunur, partiyanın
// kursu sonradan dəyişsə belə bu konkret satışın hesabı dəyişməsin deyə saxlanılır).
public class LotConsumption : ISoftDeletable
{
    public int Id { get; set; }

    public int LotId { get; set; }
    public CurrencyLot? Lot { get; set; }

    public int SellExchangeId { get; set; }
    public Exchange? SellExchange { get; set; }

    public decimal Amount { get; set; }
    public decimal Rate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

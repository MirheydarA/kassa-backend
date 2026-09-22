namespace KassaApi.Models;

// Dollar alışı ilə yaranan partiya (FIFO maya dəyəri izlənməsi üçün).
// Hər "Dollar alışı" Exchange qeydi bir partiya yaradır; sonrakı "Dollar satışı"
// qeydləri bu partiyalardan ən köhnədən başlayaraq (FIFO) çıxılır.
public class CurrencyLot : ISoftDeletable
{
    public int Id { get; set; }
    public Currency Currency { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal Rate { get; set; }

    public int SourceExchangeId { get; set; }
    public Exchange? SourceExchange { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

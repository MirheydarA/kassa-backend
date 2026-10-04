namespace KassaApi.Models;

// "Günü bitir" düyməsi basıldıqda yaradılan qeyd - Exchange/Xərclər səhifələrində
// "cari" görünüşü bu tarixdən sonrakı qeydlərlə məhdudlaşdırmaq üçün istifadə olunur
public class DayClose : ISoftDeletable
{
    public int Id { get; set; }
    public DateTime ClosedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

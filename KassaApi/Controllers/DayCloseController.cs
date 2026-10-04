using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Controllers;

// "Günü bitir": Exchange və Xərclər səhifələrinin "cari" görünüşünü sıfırlamaq üçün
// bir kəsim tarixi yaradır - köhnə məlumatlar silinmir, yalnız bu tarixdən sonrakılar
// "cari" sayılır (tarixçə ayrıca görünə bilər).
[ApiController]
[Route("api/dayclose")]
[Authorize]
public class DayCloseController : ControllerBase
{
    private readonly AppDbContext _db;

    public DayCloseController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<DayCloseDto>> GetLatest()
    {
        var latest = await _db.DayCloses
            .OrderByDescending(d => d.ClosedAt)
            .Select(d => (DateTime?)d.ClosedAt)
            .FirstOrDefaultAsync();

        return Ok(new DayCloseDto { ClosedAt = latest });
    }

    [HttpPost]
    public async Task<ActionResult<DayCloseDto>> Close()
    {
        var dayClose = new DayClose();
        _db.DayCloses.Add(dayClose);
        await _db.SaveChangesAsync();

        return Ok(new DayCloseDto { ClosedAt = dayClose.ClosedAt });
    }
}

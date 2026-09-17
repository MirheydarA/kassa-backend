using System.Security.Claims;
using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using KassaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KassaApi.Controllers;

[ApiController]
[Route("api/cashbox")]
[Authorize]
public class CashBoxController : ControllerBase
{
    private readonly CashBoxService _cashBox;
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public CashBoxController(CashBoxService cashBox, AppDbContext db, AuthService auth)
    {
        _cashBox = cashBox;
        _db = db;
        _auth = auth;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<CashBoxBalanceDto>> GetBalance() => Ok(await _cashBox.GetBalanceAsync());

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<CashBoxTransactionDto>>> GetTransactions(
        [FromQuery] string? currency, [FromQuery] string? type,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        Currency? cur = null;
        if (!string.IsNullOrWhiteSpace(currency) && Enum.TryParse<Currency>(currency, true, out var c)) cur = c;

        return Ok(await _cashBox.GetTransactionsAsync(cur, type, from, to, page, pageSize));
    }

    // Gün-gün səhifələmə: hər çağırış bir günün bütün hərəkətlərini qaytarır (böyük datasetlərdə frontend-i yükləməmək üçün).
    [HttpGet("transactions/by-day")]
    public async Task<ActionResult<CashBoxDayResultDto>> GetTransactionsByDay(
        [FromQuery] string? currency, [FromQuery] string? type,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int dayPage = 1)
    {
        Currency? cur = null;
        if (!string.IsNullOrWhiteSpace(currency) && Enum.TryParse<Currency>(currency, true, out var c)) cur = c;

        return Ok(await _cashBox.GetTransactionsByDayAsync(cur, type, from, to, dayPage));
    }

    // Bir kassa hərəkətini geri qaytarır (şifrə təsdiqi ilə)
    [HttpPost("transactions/{id}/revert")]
    public async Task<ActionResult> RevertTransaction(int id, RevertTransactionRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var user = _db.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return Unauthorized();

        if (string.IsNullOrEmpty(request.Password) || !_auth.VerifyPassword(user, request.Password))
            return BadRequest(new { message = "Şifrə yanlışdır" });

        var reverted = await _cashBox.RevertTransactionAsync(id);
        if (reverted == null) return NotFound();

        await _db.SaveChangesAsync();

        return Ok(new { message = "Əməliyyat geri qaytarıldı" });
    }
}

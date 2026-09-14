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

    public CashBoxController(CashBoxService cashBox)
    {
        _cashBox = cashBox;
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

        TransactionType? t = null;
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<TransactionType>(type, true, out var tt)) t = tt;

        return Ok(await _cashBox.GetTransactionsAsync(cur, t, from, to, page, pageSize));
    }
}

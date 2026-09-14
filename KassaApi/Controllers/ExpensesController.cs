using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using KassaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Controllers;

[ApiController]
[Route("api/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CashBoxService _cashBox;

    public ExpensesController(AppDbContext db, CashBoxService cashBox)
    {
        _db = db;
        _cashBox = cashBox;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Expenses.OrderByDescending(e => e.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResult<ExpenseDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Məbləğ 0-dan böyük olmalıdır" });

        var expense = new Expense { Amount = request.Amount, Description = request.Description };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        await _cashBox.ChangeBalanceAsync(Currency.RUB, -request.Amount, CashSource.Expense, expense.Id,
            $"Xərc: {request.Description}");

        await _db.SaveChangesAsync();

        return Ok(ToDto(expense));
    }

    private static ExpenseDto ToDto(Expense e) => new()
    {
        Id = e.Id,
        Amount = e.Amount,
        Description = e.Description,
        CreatedAt = e.CreatedAt
    };

    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseDto>> Update(int id, UpdateExpenseRequest request)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        if (request.Amount <= 0)
            return BadRequest(new { message = "Məbləğ 0-dan böyük olmalıdır" });

        await _cashBox.ChangeBalanceAsync(Currency.RUB, expense.Amount, CashSource.Expense, expense.Id,
            $"Xərc redaktəsi (geri alma): {expense.Description}");

        expense.Amount = request.Amount;
        expense.Description = request.Description;

        await _cashBox.ChangeBalanceAsync(Currency.RUB, -request.Amount, CashSource.Expense, expense.Id,
            $"Xərc redaktəsi (yeni): {expense.Description}");

        await _db.SaveChangesAsync();
        return Ok(ToDto(expense));
    }
}

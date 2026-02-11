using ExpenseTracker.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    private bool TryGetUserId(out Guid userId)
    {
        userId = default;
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] int year, [FromQuery] int month)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (month < 1 || month > 12) return BadRequest(new { error = "Invalid month" });

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var incomeList = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end && t.Type == 1)
            .Select(t => t.Amount)
            .ToListAsync();

        var expenseList = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end && t.Type == 0)
            .Select(t => t.Amount)
            .ToListAsync();

        var income = incomeList.Sum();
        var expense = expenseList.Sum();

        return Ok(new { income, expense, balance = income - expense });
    }

    [HttpGet("by-category")]
    public async Task<IActionResult> ByCategory([FromQuery] int year, [FromQuery] int month, [FromQuery] int type = 0)
    {
        // type: 0 = Expense, 1 = Income
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (month < 1 || month > 12) return BadRequest(new { error = "Invalid month" });
        if (type is not (0 or 1)) return BadRequest(new { error = "Invalid type" });

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        // IMPORTANT: SQLite + decimal Sum => правим групиране в C#
        var rows = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= start && t.Date < end && t.Type == type)
            .Include(t => t.Category)
            .Select(t => new
            {
                t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : "Uncategorized",
                CategoryColor = t.Category != null ? t.Category.Color : null,
                t.Amount
            })
            .ToListAsync();

        var result = rows
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.CategoryColor })
            .Select(g => new
            {
                categoryId = g.Key.CategoryId,
                categoryName = g.Key.CategoryName,
                categoryColor = g.Key.CategoryColor,
                total = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.total)
            .ToList();

        return Ok(result);
    }


    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] int take = 10)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        take = Math.Clamp(take, 1, 50);

        var since = DateTime.UtcNow.AddDays(-1);

        var items = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreatedAt >= since)
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .Select(t => new
            {
                id = t.Id,
                date = t.Date,
                type = t.Type,
                amount = t.Amount,
                note = t.Note,
                categoryId = t.CategoryId,
                categoryName = t.Category != null ? t.Category.Name : null,
                createdAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}
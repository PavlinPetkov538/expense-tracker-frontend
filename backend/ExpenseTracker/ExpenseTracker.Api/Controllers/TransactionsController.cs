using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WorkspaceContext _wctx;

    public TransactionsController(AppDbContext db, WorkspaceContext wctx)
    {
        _db = db;
        _wctx = wctx;
    }

    private Guid UserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(raw, out var id))
                throw new UnauthorizedAccessException("Invalid user id claim.");
            return id;
        }
    }

    private async Task<Guid> WorkspaceIdAsync()
    {
        var header = Request.Headers["X-Workspace-Id"].ToString();
        return await _wctx.ResolveWorkspaceIdAsync(UserId, header);
    }

    public record TransactionDto(
        Guid Id,
        decimal Amount,
        DateTime Date,
        int Type,
        string? Note,
        Guid? CategoryId,
        string? CategoryName,
        string? CategoryColor,
        DateTime CreatedAt
    );

    public record CreateTransactionRequest(
        decimal Amount,
        DateTime Date,
        int Type,
        string? Note,
        Guid? CategoryId
    );

    public record UpdateTransactionRequest(
        decimal Amount,
        DateTime Date,
        int Type,
        string? Note,
        Guid? CategoryId
    );

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int take = 50)
    {
        var workspaceId = await WorkspaceIdAsync();
        take = Math.Clamp(take, 1, 200);

        var items = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(take)
            .Select(t => new
            {
                id = t.Id,
                amount = t.Amount,
                date = t.Date,
                type = t.Type,
                note = t.Note,
                categoryId = t.CategoryId,
                categoryName = t.Category != null ? t.Category.Name : null,
                categoryColor = t.Category != null ? t.Category.Color : null,
                createdAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetById(Guid id)
    {
        var workspaceId = await WorkspaceIdAsync();

        var item = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id && t.WorkspaceId == workspaceId)
            .Include(t => t.Category)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Date,
                t.Type,
                t.Note,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null,
                t.Category != null ? t.Category.Color : null,
                t.CreatedAt
            ))
            .FirstOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionRequest req)
    {
        var workspaceId = await WorkspaceIdAsync();

        if (req.Amount <= 0) return BadRequest(new { error = "Amount must be > 0." });
        if (req.Type is not (0 or 1)) return BadRequest(new { error = "Invalid transaction type." });

        if (req.CategoryId != null)
        {
            var catOk = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId && c.WorkspaceId == workspaceId);
            if (!catOk) return BadRequest(new { error = "Invalid category." });
        }

        var entity = new Transaction
        {
            UserId = UserId,
            WorkspaceId = workspaceId,
            CreatedByUserId = UserId,
            Amount = req.Amount,
            Date = req.Date.Date,
            Type = req.Type,
            Note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim(),
            CategoryId = req.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(entity);
        await _db.SaveChangesAsync();

        var created = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == entity.Id && t.WorkspaceId == workspaceId)
            .Include(t => t.Category)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Date,
                t.Type,
                t.Note,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null,
                t.Category != null ? t.Category.Color : null,
                t.CreatedAt
            ))
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Update(Guid id, [FromBody] UpdateTransactionRequest req)
    {
        var workspaceId = await WorkspaceIdAsync();

        if (req.Amount <= 0) return BadRequest(new { error = "Amount must be > 0." });
        if (req.Type is not (0 or 1)) return BadRequest(new { error = "Invalid transaction type." });

        if (req.CategoryId != null)
        {
            var catOk = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId && c.WorkspaceId == workspaceId);
            if (!catOk) return BadRequest(new { error = "Invalid category." });
        }

        var entity = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.WorkspaceId == workspaceId);
        if (entity is null) return NotFound();

        entity.Amount = req.Amount;
        entity.Date = req.Date.Date;
        entity.Type = req.Type;
        entity.Note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
        entity.CategoryId = req.CategoryId;

        await _db.SaveChangesAsync();

        var dto = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id && t.WorkspaceId == workspaceId)
            .Include(t => t.Category)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Date,
                t.Type,
                t.Note,
                t.CategoryId,
                t.Category != null ? t.Category.Name : null,
                t.Category != null ? t.Category.Color : null,
                t.CreatedAt
            ))
            .FirstAsync();

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var workspaceId = await WorkspaceIdAsync();

        var entity = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.WorkspaceId == workspaceId);
        if (entity is null) return NotFound();

        _db.Transactions.Remove(entity);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? categoryName,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] int take = 200
    )
    {
        var workspaceId = await WorkspaceIdAsync();
        take = Math.Clamp(take, 1, 500);

        var query = _db.Transactions
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var term = categoryName.Trim();
            query = query.Where(t => t.Category != null && EF.Functions.Like(t.Category.Name, $"%{term}%"));
        }

        if (createdFrom.HasValue)
            query = query.Where(t => t.CreatedAt >= createdFrom.Value);

        if (createdTo.HasValue)
        {
            var end = createdTo.Value.Date.AddDays(1);
            query = query.Where(t => t.CreatedAt < end);
        }

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .Select(t => new
            {
                id = t.Id,
                amount = t.Amount,
                date = t.Date,
                type = t.Type,
                note = t.Note,
                categoryId = t.CategoryId,
                categoryName = t.Category != null ? t.Category.Name : null,
                categoryColor = t.Category != null ? t.Category.Color : null,
                createdAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}
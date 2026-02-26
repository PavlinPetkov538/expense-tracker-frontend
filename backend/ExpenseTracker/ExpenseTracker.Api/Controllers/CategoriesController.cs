using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Data.Entities;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WorkspaceContext _wctx;

    public CategoriesController(AppDbContext db, WorkspaceContext wctx)
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

    public record CategoryDto(Guid Id, string Name, int Type, string? Color, DateTime CreatedAt);
    public record CreateCategoryRequest(string Name, int Type, string? Color);
    public record UpdateCategoryRequest(string Name, int Type, string? Color);

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var workspaceId = await WorkspaceIdAsync();

        var items = await _db.Categories
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Color, c.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
    {
        var workspaceId = await WorkspaceIdAsync();

        var item = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && c.WorkspaceId == workspaceId)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Color, c.CreatedAt))
            .FirstOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest req)
    {
        var workspaceId = await WorkspaceIdAsync();

        var name = (req.Name ?? "").Trim();
        if (name.Length == 0) return BadRequest(new { error = "Name is required." });
        if (name.Length > 100) return BadRequest(new { error = "Name is too long (max 100)." });
        if (req.Type < 0 || req.Type > 2) return BadRequest(new { error = "Invalid category type." });

        var exists = await _db.Categories.AnyAsync(c => c.WorkspaceId == workspaceId && c.Name == name);
        if (exists) return Conflict(new { error = "Category with this name already exists in this account." });

        var entity = new Category
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = UserId,
            UserId = UserId,
            Name = name,
            Type = req.Type,
            Color = string.IsNullOrWhiteSpace(req.Color) ? null : req.Color,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(entity);
        await _db.SaveChangesAsync();

        var dto = new CategoryDto(entity.Id, entity.Name, entity.Type, entity.Color, entity.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, [FromBody] UpdateCategoryRequest req)
    {
        var workspaceId = await WorkspaceIdAsync();

        var name = (req.Name ?? "").Trim();
        if (name.Length == 0) return BadRequest(new { error = "Name is required." });
        if (name.Length > 100) return BadRequest(new { error = "Name is too long (max 100)." });
        if (req.Type < 0 || req.Type > 2) return BadRequest(new { error = "Invalid category type." });

        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.WorkspaceId == workspaceId);
        if (entity is null) return NotFound();

        var exists = await _db.Categories.AnyAsync(c => c.WorkspaceId == workspaceId && c.Name == name && c.Id != id);
        if (exists) return Conflict(new { error = "Category with this name already exists in this account." });

        entity.Name = name;
        entity.Type = req.Type;
        entity.Color = string.IsNullOrWhiteSpace(req.Color) ? null : req.Color;

        await _db.SaveChangesAsync();

        var dto = new CategoryDto(entity.Id, entity.Name, entity.Type, entity.Color, entity.CreatedAt);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var workspaceId = await WorkspaceIdAsync();

        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.WorkspaceId == workspaceId);
        if (entity is null) return NotFound();

        var hasTx = await _db.Transactions.AnyAsync(t => t.WorkspaceId == workspaceId && t.CategoryId == id);
        if (hasTx) return Conflict(new { error = "Cannot delete category that has transactions." });

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
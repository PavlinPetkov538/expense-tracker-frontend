using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // JWT по default (след като оправихме Program.cs)
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

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

    public record CategoryDto(Guid Id, string Name, int Type, string? Color, DateTime CreatedAt);
    public record CreateCategoryRequest(string Name, int Type, string? Color);
    public record UpdateCategoryRequest(string Name, int Type, string? Color);

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == UserId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Color, c.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
    {
        var item = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && c.UserId == UserId)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Color, c.CreatedAt))
            .FirstOrDefaultAsync();

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest req)
    {
        var name = (req.Name ?? "").Trim();
        if (name.Length == 0) return BadRequest(new { error = "Name is required." });
        if (name.Length > 100) return BadRequest(new { error = "Name is too long (max 100)." });
        if (req.Type < 0 || req.Type > 2) return BadRequest(new { error = "Invalid category type." });

        // optional: уникално име за user
        var exists = await _db.Categories.AnyAsync(c => c.UserId == UserId && c.Name == name);
        if (exists) return Conflict(new { error = "Category with this name already exists." });

        var entity = new Category
        {
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
        var name = (req.Name ?? "").Trim();
        if (name.Length == 0) return BadRequest(new { error = "Name is required." });
        if (name.Length > 100) return BadRequest(new { error = "Name is too long (max 100)." });
        if (req.Type < 0 || req.Type > 2) return BadRequest(new { error = "Invalid category type." });

        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (entity is null) return NotFound();

        // optional: уникално име за user (без самия запис)
        var exists = await _db.Categories.AnyAsync(c => c.UserId == UserId && c.Name == name && c.Id != id);
        if (exists) return Conflict(new { error = "Category with this name already exists." });

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
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (entity is null) return NotFound();

        // За да не чупим транзакции: ако има транзакции с тази категория -> 409
        var hasTx = await _db.Transactions.AnyAsync(t => t.UserId == UserId && t.CategoryId == id);
        if (hasTx) return Conflict(new { error = "Cannot delete category that has transactions." });

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
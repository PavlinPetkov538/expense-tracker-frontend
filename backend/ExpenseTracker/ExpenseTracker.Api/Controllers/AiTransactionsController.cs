using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Data.Entities;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/ai/transactions")]
[Authorize]
public class AiTransactionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WorkspaceContext _wctx;
    private readonly OpenAiReceiptService _ai;

    public AiTransactionsController(AppDbContext db, WorkspaceContext wctx, OpenAiReceiptService ai)
    {
        _db = db;
        _wctx = wctx;
        _ai = ai;
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

    public record FromTextRequest(string Text);

    public class FromReceiptRequest
    {
        // Имената "file" и "extraNote" са тези, които Swagger UI ще прати
        // (можеш да ги държиш така за да е удобно на фронтенда)
        [FromForm(Name = "file")]
        public IFormFile File { get; set; } = default!;

        [FromForm(Name = "extraNote")]
        public string? ExtraNote { get; set; }
    }

    [HttpPost("from-text")]
    public async Task<IActionResult> FromText([FromBody] FromTextRequest req)
    {
        var wid = await WorkspaceIdAsync();
        var extracted = await _ai.ExtractFromTextAsync(req.Text);

        return Ok(await CreateTransactionAsync(wid, extracted));
    }

    [HttpPost("from-receipt")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> FromReceipt([FromForm] FromReceiptRequest req)
    {
        if (req?.File == null || req.File.Length == 0)
            return BadRequest("Missing file.");

        var wid = await WorkspaceIdAsync();

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await req.File.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        var extracted = await _ai.ExtractFromFileAsync(bytes, req.File.ContentType, req.ExtraNote);

        return Ok(await CreateTransactionAsync(wid, extracted));
    }

    private async Task<object> CreateTransactionAsync(Guid workspaceId, OpenAiReceiptService.AiTx extracted)
    {
        var date = DateTime.UtcNow.Date;
        if (DateTime.TryParseExact(
                extracted.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            date = parsed.Date;
        }

        var catName = (extracted.CategoryName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(catName)) catName = "Other";

        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Name == catName);
        if (cat == null)
        {
            cat = new Category
            {
                WorkspaceId = workspaceId,
                CreatedByUserId = UserId,
                UserId = UserId,
                Name = catName,
                Type = extracted.Type == "income" ? (int)CategoryType.Income : (int)CategoryType.Expense
            };

            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
        }

        var tx = new Transaction
        {
            WorkspaceId = workspaceId,
            UserId = UserId,
            CreatedByUserId = UserId,
            Amount = extracted.Amount,
            Date = date,
            Type = extracted.Type == "income" ? 1 : 0,
            Note = string.IsNullOrWhiteSpace(extracted.Note) ? extracted.Merchant : extracted.Note,
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        return new
        {
            id = tx.Id,
            amount = tx.Amount,
            date = tx.Date,
            type = tx.Type,
            category = cat.Name,
            note = tx.Note,
            confidence = extracted.Confidence
        };
    }
}
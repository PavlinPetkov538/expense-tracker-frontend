using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Data.Entities;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspacesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WorkspaceContext _wctx;

    public WorkspacesController(AppDbContext db, WorkspaceContext wctx)
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

    private string? UserEmail => User.FindFirstValue(ClaimTypes.Email);

    public record CreateWorkspaceRequest(string Name);
    public record InviteRequest(string Email);
    public record AcceptRejectRequest(string Token);

    // ✅ used by Settings to load accounts; ensures Personal exists
    [HttpGet("me")]
    public async Task<IActionResult> MyWorkspaces()
    {
        await _wctx.GetOrCreatePersonalWorkspaceAsync(UserId);

        var items = await _db.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.Workspace)
            .Where(m => m.UserId == UserId)
            .OrderByDescending(m => m.IsOwner)
            .ThenBy(m => m.Workspace.Name)
            .Select(m => new
            {
                workspaceId = m.WorkspaceId,
                name = m.Workspace.Name,
                isOwner = m.IsOwner
            })
            .ToListAsync();

        return Ok(items);
    }

    // ✅ create Family/Master account
    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest req)
    {
        var name = (req.Name ?? "").Trim();
        if (name.Length < 2) return BadRequest("Name too short.");

        var ws = new Workspace { Name = name, OwnerId = UserId };
        _db.Workspaces.Add(ws);

        _db.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = ws.Id,
            UserId = UserId,
            IsOwner = true
        });

        await _db.SaveChangesAsync();
        return Ok(new { workspaceId = ws.Id, name = ws.Name, isOwner = true });
    }

    // ✅ invite to family; ONLY owner can invite (inviter is master)
    [HttpPost("{workspaceId:guid}/invite")]
    public async Task<IActionResult> Invite(Guid workspaceId, [FromBody] InviteRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (email.Length < 5 || !email.Contains("@")) return BadRequest("Invalid email.");

        if (!await _wctx.IsOwnerAsync(UserId, workspaceId))
            return Forbid();

        var existing = await _db.WorkspaceInvites
            .AsNoTracking()
            .AnyAsync(i => i.WorkspaceId == workspaceId
                        && i.InvitedEmail == email
                        && i.AcceptedAt == null
                        && i.RejectedAt == null
                        && i.ExpiresAt > DateTime.UtcNow);

        if (existing) return BadRequest("Invite already pending for this email.");

        var token = Guid.NewGuid().ToString("N");

        var invite = new WorkspaceInvite
        {
            WorkspaceId = workspaceId,
            InvitedEmail = email,
            Token = token,
            InvitedByUserId = UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.WorkspaceInvites.Add(invite);
        await _db.SaveChangesAsync();

        // For now return token so you can test; later you email it.
        return Ok(new { token, expiresAt = invite.ExpiresAt });
    }

    // ✅ invited users see pending invites here
    [HttpGet("invites")]
    public async Task<IActionResult> MyInvites()
    {
        var email = (UserEmail ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return Ok(Array.Empty<object>());

        var now = DateTime.UtcNow;

        var items = await _db.WorkspaceInvites
            .AsNoTracking()
            .Include(i => i.Workspace)
            .Where(i => i.InvitedEmail == email
                        && i.AcceptedAt == null
                        && i.RejectedAt == null
                        && i.ExpiresAt > now)
            .OrderByDescending(i => i.ExpiresAt)
            .Select(i => new
            {
                token = i.Token,
                workspaceId = i.WorkspaceId,
                workspaceName = i.Workspace.Name,
                expiresAt = i.ExpiresAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptRejectRequest req)
    {
        var email = (UserEmail ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("Missing email claim.");

        var inv = await _db.WorkspaceInvites
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.Token == req.Token);

        if (inv == null) return NotFound("Invalid token.");
        if (inv.ExpiresAt <= DateTime.UtcNow) return BadRequest("Invite expired.");
        if (inv.AcceptedAt != null || inv.RejectedAt != null) return BadRequest("Invite already handled.");
        if (inv.InvitedEmail != email) return Forbid();

        var exists = await _db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == inv.WorkspaceId && m.UserId == UserId);
        if (!exists)
        {
            _db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = inv.WorkspaceId,
                UserId = UserId,
                IsOwner = false
            });
        }

        inv.AcceptedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { workspaceId = inv.WorkspaceId, name = inv.Workspace.Name, isOwner = false });
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] AcceptRejectRequest req)
    {
        var email = (UserEmail ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("Missing email claim.");

        var inv = await _db.WorkspaceInvites.FirstOrDefaultAsync(i => i.Token == req.Token);

        if (inv == null) return NotFound("Invalid token.");
        if (inv.ExpiresAt <= DateTime.UtcNow) return BadRequest("Invite expired.");
        if (inv.AcceptedAt != null || inv.RejectedAt != null) return BadRequest("Invite already handled.");
        if (inv.InvitedEmail != email) return Forbid();

        inv.RejectedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok();
    }
}
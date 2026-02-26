using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public class WorkspaceContext
{
    private readonly AppDbContext _db;

    public WorkspaceContext(AppDbContext db) => _db = db;

    public async Task<Workspace> GetOrCreatePersonalWorkspaceAsync(Guid userId)
    {
        var existing = await _db.Workspaces
            .AsNoTracking()
            .Where(w => w.OwnerId == userId && w.Name == "Personal")
            .FirstOrDefaultAsync();

        if (existing != null) return existing;

        var ws = new Workspace { Name = "Personal", OwnerId = userId };
        _db.Workspaces.Add(ws);

        _db.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = ws.Id,
            UserId = userId,
            IsOwner = true
        });

        await _db.SaveChangesAsync();
        return ws;
    }

    public async Task<Guid> ResolveWorkspaceIdAsync(Guid userId, string? headerWorkspaceId)
    {
        if (Guid.TryParse(headerWorkspaceId, out var wid))
        {
            var isMember = await _db.WorkspaceMembers
                .AsNoTracking()
                .AnyAsync(m => m.WorkspaceId == wid && m.UserId == userId);

            if (isMember) return wid;
        }

        var personal = await GetOrCreatePersonalWorkspaceAsync(userId);
        return personal.Id;
    }

    public async Task<bool> IsOwnerAsync(Guid userId, Guid workspaceId)
    {
        return await _db.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsOwner);
    }
}
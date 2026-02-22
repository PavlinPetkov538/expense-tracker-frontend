namespace ExpenseTracker.Api.Data.Entities;

public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsOwner { get; set; } = false;
}
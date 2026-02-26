namespace ExpenseTracker.Api.Data.Entities;

public class WorkspaceInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public string InvitedEmail { get; set; } = "";

    public string Token { get; set; } = "";

    public Guid InvitedByUserId { get; set; }
    public User InvitedByUser { get; set; } = null!;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
}
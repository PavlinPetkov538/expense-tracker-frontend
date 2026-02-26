using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Data.Entities;

public class WorkspaceMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsOwner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
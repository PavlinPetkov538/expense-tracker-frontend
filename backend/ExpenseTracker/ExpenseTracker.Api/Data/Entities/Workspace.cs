namespace ExpenseTracker.Api.Data.Entities;

public class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
}
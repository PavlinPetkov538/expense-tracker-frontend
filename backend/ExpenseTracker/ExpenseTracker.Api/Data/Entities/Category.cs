namespace ExpenseTracker.Api.Data.Entities;

public enum CategoryType { Expense = 0, Income = 1, Both = 2 }

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ✅ NEW
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    // ✅ NEW
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    // kept for backward compatibility if you already have it in DB / code
    public Guid UserId { get; set; }

    public string Name { get; set; } = "";
    public int Type { get; set; }              // 0 Expense / 1 Income / 2 Both
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
namespace ExpenseTracker.Api.Data.Entities;

public enum CategoryType { Expense = 0, Income = 1, Both = 2 }

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }           // <- важно
    public string Name { get; set; } = "";
    public int Type { get; set; }              // Expense/Income/Both ако ползваш enum
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
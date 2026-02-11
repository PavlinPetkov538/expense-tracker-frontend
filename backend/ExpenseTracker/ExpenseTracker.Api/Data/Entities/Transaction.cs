namespace ExpenseTracker.Api.Data.Entities;

public enum TransactionType { Expense = 0, Income = 1 }

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }           // <- важно

    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int Type { get; set; }              // Expense/Income ако ползваш enum
    public string? Note { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

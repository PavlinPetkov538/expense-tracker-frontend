using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Api.Data.Entities;

public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = "";
}
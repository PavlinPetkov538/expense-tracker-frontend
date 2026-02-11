namespace ExpenseTracker.Api.Services.AuthenticationService;

public interface IAuthenticationService
{
    Task<string> GenerateToken(Guid userId);
}
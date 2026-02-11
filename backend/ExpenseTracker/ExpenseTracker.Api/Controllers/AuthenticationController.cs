using ExpenseTracker.Api.Data.Entities;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExpenseTracker.Api.Services.AuthenticationService;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService authenticationService;
    private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signInManager;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        this.authenticationService = authenticationService;
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
    {
        var user = new User
        {
            Email = registerModel.Email,
            FullName = registerModel.FullName,
            UserName = registerModel.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerModel.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        string token = await authenticationService.GenerateToken(user.Id);
        return Ok(new { token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
    {
        var user = await userManager.FindByEmailAsync(loginModel.Email);
        if (user == null) return BadRequest(new { error = "Invalid credentials" });

        var result = await signInManager.CheckPasswordSignInAsync(user, loginModel.Password, false);
        if (!result.Succeeded) return BadRequest(new { error = "Invalid credentials" });

        string token = await authenticationService.GenerateToken(user.Id);
        return Ok(new { token });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email)
        });
    }
}
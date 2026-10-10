using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SignalR.Api.Auth;
using SignalR.BLL.Auth;

namespace SignalR.Api.Controllers;

public sealed record LoginResponse(
    string Token,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> Roles,
    DateTime ExpiresAt);

public sealed class LoginRequest
{
    [Required]
    public string UserName { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly JwtTokenService _tokens;

    public AuthController(IAuthService auth, JwtTokenService tokens)
    {
        _auth = auth;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _auth.AuthenticateAsync(request.UserName, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var ticket = _tokens.Create(user);
        return Ok(new LoginResponse(ticket.Token, user.UserName, user.DisplayName, user.Roles, ticket.ExpiresAt));
    }
}

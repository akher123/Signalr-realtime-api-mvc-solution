using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SignalR.BLL.Auth;

namespace SignalR.Api.Auth;

public sealed record LoginTicket(string Token, DateTime ExpiresAt);

public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginTicket Create(AuthenticatedUser user)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "SignalR.Api";
        var audience = _configuration["Jwt:Audience"] ?? "SignalR.Mvc";
        var minutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var parsed) ? parsed : 480;
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();
        return new LoginTicket(handler.WriteToken(token), expires);
    }
}

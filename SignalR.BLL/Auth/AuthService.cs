using Microsoft.EntityFrameworkCore;
using SignalR.DAL.Data;

namespace SignalR.BLL.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken)
    {
        var normalized = userName.Trim().ToLowerInvariant();
        var user = await _db.Users
            .AsNoTracking()
            .Include(candidate => candidate.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(candidate => candidate.UserName.ToLower() == normalized, cancellationToken);

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var roles = user.UserRoles
            .Select(userRole => userRole.Role.Name)
            .OrderBy(name => name)
            .ToArray();

        return new AuthenticatedUser(user.Id, user.UserName, user.DisplayName, roles);
    }
}

namespace SignalR.BLL.Auth;

public sealed record AuthenticatedUser(Guid Id, string UserName, string DisplayName, IReadOnlyList<string> Roles);

public interface IAuthService
{
    Task<AuthenticatedUser?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken);
}

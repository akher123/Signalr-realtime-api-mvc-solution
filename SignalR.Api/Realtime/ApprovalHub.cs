using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.Api.Realtime;

[Authorize]
public sealed class ApprovalHub : Hub
{
    public const string Route = "/hubs/approvals";

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId(Context.User);
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.ForUser(userId.Value));
        }

        foreach (var role in GetRoles(Context.User))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.ForRole(role));
        }

        await base.OnConnectedAsync();
    }

    private static Guid? GetUserId(ClaimsPrincipal? user)
    {
        var value = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static IEnumerable<string> GetRoles(ClaimsPrincipal? user) =>
        user?.FindAll(ClaimTypes.Role).Select(claim => claim.Value) ?? [];
}

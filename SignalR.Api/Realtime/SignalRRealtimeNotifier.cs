using Microsoft.AspNetCore.SignalR;
using SignalR.BLL.Realtime;

namespace SignalR.Api.Realtime;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<ApprovalHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<ApprovalHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushToUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(HubGroups.ForUser(userId)).SendAsync(eventName, payload, cancellationToken);

    public Task PushToRoleAsync(string role, string eventName, object payload, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(HubGroups.ForRole(role)).SendAsync(eventName, payload, cancellationToken);
}

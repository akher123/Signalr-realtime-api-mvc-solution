namespace SignalR.BLL.Realtime;

public interface IRealtimeNotifier
{
    Task PushToUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken);

    Task PushToRoleAsync(string role, string eventName, object payload, CancellationToken cancellationToken);
}

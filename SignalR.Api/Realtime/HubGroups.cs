namespace SignalR.Api.Realtime;

public static class HubGroups
{
    public static string ForUser(Guid userId) => $"user:{userId}";

    public static string ForRole(string role) => $"role:{role}";
}

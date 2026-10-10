namespace SignalR.DAL.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<ApprovalRequest> Requests { get; set; } = new List<ApprovalRequest>();
}

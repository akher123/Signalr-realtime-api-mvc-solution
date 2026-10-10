namespace SignalR.DAL.Entities;

public sealed class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

using Microsoft.EntityFrameworkCore;
using SignalR.DAL.Entities;

namespace SignalR.DAL.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.UserName).IsUnique();
            entity.Property(user => user.UserName).HasMaxLength(64);
            entity.Property(user => user.DisplayName).HasMaxLength(128);
            entity.Property(user => user.PasswordHash).HasMaxLength(200);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(role => role.Name).IsUnique();
            entity.Property(role => role.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId);
            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId);
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.Property(request => request.Title).HasMaxLength(200);
            entity.Property(request => request.Details).HasMaxLength(2000);
            entity.Property(request => request.DecisionNote).HasMaxLength(1000);
            entity.HasOne(request => request.Requester)
                .WithMany(user => user.Requests)
                .HasForeignKey(request => request.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.DecidedBy)
                .WithMany()
                .HasForeignKey(request => request.DecidedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

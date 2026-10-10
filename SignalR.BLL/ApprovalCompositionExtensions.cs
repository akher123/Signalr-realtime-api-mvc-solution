using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalR.BLL.Approvals;
using SignalR.BLL.Auth;
using SignalR.DAL.Data;
using SignalR.DAL.Entities;

namespace SignalR.BLL;

public static class ApprovalCompositionExtensions
{
    public static IServiceCollection AddApprovalCore(this IServiceCollection services, string connectionString)
    {
        services.AddApprovalData(connectionString);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        return services;
    }

    public static async Task InitializeApprovalDatabaseAsync(this IServiceProvider services, bool seed)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        if (seed)
        {
            await DevelopmentSeeder.SeedAsync(db);
        }
    }
}

public static class DevelopmentSeeder
{
    public const string Password = "Password123!";

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        var requesterRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Requester };
        var approverRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Approver };
        var requester = new User
        {
            Id = Guid.NewGuid(),
            UserName = "requester",
            DisplayName = "Request User",
            PasswordHash = PasswordHasher.Hash(Password)
        };
        var approver = new User
        {
            Id = Guid.NewGuid(),
            UserName = "approver",
            DisplayName = "Approval Manager",
            PasswordHash = PasswordHasher.Hash(Password)
        };

        db.Roles.AddRange(requesterRole, approverRole);
        db.Users.AddRange(requester, approver);
        db.UserRoles.AddRange(
            new UserRole { UserId = requester.Id, RoleId = requesterRole.Id },
            new UserRole { UserId = approver.Id, RoleId = approverRole.Id });

        await db.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SignalR.DAL.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddApprovalData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        return services;
    }
}

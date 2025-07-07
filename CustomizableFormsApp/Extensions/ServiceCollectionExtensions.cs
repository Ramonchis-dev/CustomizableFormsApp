using CustomizableFormsApp.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomizableFormsApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');

            connectionString =
                $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};" +
                $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
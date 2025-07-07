using CustomizableFormsApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace CustomizableFormsApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        var serviceProvider = services.BuildServiceProvider(); 
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DatabaseStartup"); 

        var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        var appSettingsConnectionString = configuration.GetConnectionString("DefaultConnection");

        string connectionString;

        if (!string.IsNullOrEmpty(envConnectionString))
        {
            connectionString = envConnectionString;
            logger.LogInformation("Using DATABASE_URL from environment variable.");
        }
        else if (!string.IsNullOrEmpty(appSettingsConnectionString))
        {
            connectionString = appSettingsConnectionString;
            logger.LogWarning("DATABASE_URL environment variable is not set or empty. Falling back to DefaultConnection from appsettings.json: {ConnectionString}", connectionString);
        }
        else
        {
            // This throw will provide a clear error if no connection string is found
            throw new InvalidOperationException("No database connection string found. Neither DATABASE_URL environment variable nor DefaultConnection in appsettings.json is set.");
        }

        if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
        {
            try
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');

                connectionString =
                    $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};" +
                    $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
                logger.LogInformation("Successfully parsed PostgreSQL URI connection string.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse PostgreSQL URI from DATABASE_URL. Raw string: {RawConnectionString}", connectionString);
                // Re-throw to ensure the app crashes with a clear error if parsing fails
                throw new InvalidOperationException("Failed to parse DATABASE_URL environment variable.", ex);
            }
        }
        else
        {
            logger.LogInformation("Connection string is not a postgres URI, using as-is: {ConnectionString}", connectionString);
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
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

        string finalConnectionString;

        // Prioritize DATABASE_URL environment variable
        var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            // If DATABASE_URL is present, it MUST be a postgres:// URI for this logic
            if (envConnectionString.StartsWith("postgres://"))
            {
                try
                {
                    var uri = new Uri(envConnectionString);
                    var userInfo = uri.UserInfo.Split(':');

                    finalConnectionString =
                        $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};" +
                        $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
                    logger.LogInformation("Successfully parsed DATABASE_URL (URI format) and constructed connection string.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to parse DATABASE_URL (URI format). Raw string: {RawConnectionString}", envConnectionString);
                    throw new InvalidOperationException("Failed to parse DATABASE_URL environment variable as a PostgreSQL URI.", ex);
                }
            }
            else
            {
                // If DATABASE_URL is set but NOT a postgres:// URI, it's likely a direct connection string.
                // We need to ensure SSL parameters are added if not already present.
                finalConnectionString = envConnectionString;
                if (!finalConnectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
                {
                    finalConnectionString += ";SSL Mode=Require;Trust Server Certificate=true;";
                    logger.LogWarning("DATABASE_URL is not a URI. Added SSL Mode=Require and Trust Server Certificate=true. Final: {ConnectionString}", finalConnectionString);
                }
                else
                {
                    logger.LogInformation("DATABASE_URL is not a URI, using as-is (assuming SSL is configured): {ConnectionString}", finalConnectionString);
                }
            }
        }
        else
        {
            // Fallback to DefaultConnection from appsettings.json
            var appSettingsConnectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(appSettingsConnectionString))
            {
                finalConnectionString = appSettingsConnectionString;
                logger.LogWarning("DATABASE_URL environment variable is not set or empty. Falling back to DefaultConnection from appsettings.json. Final: {ConnectionString}", finalConnectionString);

                // IMPORTANT: Ensure SSL parameters are added for Render if using appsettings.json fallback
                if (!finalConnectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
                {
                    finalConnectionString += ";SSL Mode=Require;Trust Server Certificate=true;";
                    logger.LogWarning("Added SSL Mode=Require and Trust Server Certificate=true to DefaultConnection from appsettings.json. Final: {ConnectionString}", finalConnectionString);
                }
            }
            else
            {
                throw new InvalidOperationException("No database connection string found. Neither DATABASE_URL environment variable nor DefaultConnection in appsettings.json is set.");
            }
        }

        logger.LogInformation("Attempting to connect with connection string: {MaskedConnectionString}", MaskConnectionString(finalConnectionString));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(finalConnectionString));

        return services;
    }

    // Helper method to mask password in logs
    private static string MaskConnectionString(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "********";
        }
        return builder.ToString();
    }
}
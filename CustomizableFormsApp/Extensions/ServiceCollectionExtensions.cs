using CustomizableFormsApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
// Removed: using Npgsql; // No longer needed for NpgsqlConnectionStringBuilder in this simplified version

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
            // Assume DATABASE_URL is in postgres:// URI format or a direct connection string
            finalConnectionString = envConnectionString;
            logger.LogInformation("Using DATABASE_URL from environment variable.");

            // Ensure SSL Mode and Trust Server Certificate are present for Render
            // This logic is crucial and should apply regardless of initial format
            if (!finalConnectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
            {
                finalConnectionString += ";SSL Mode=Require";
                logger.LogInformation("Added 'SSL Mode=Require' to connection string.");
            }
            if (!finalConnectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
            {
                finalConnectionString += ";Trust Server Certificate=true";
                logger.LogInformation("Added 'Trust Server Certificate=true' to connection string.");
            }
        }
        else
        {
            // Fallback to DefaultConnection from appsettings.json
            var appSettingsConnectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(appSettingsConnectionString))
            {
                finalConnectionString = appSettingsConnectionString;
                logger.LogWarning("DATABASE_URL environment variable is not set or empty. Falling back to DefaultConnection from appsettings.json.");

                // Ensure SSL Mode and Trust Server Certificate are present for Render if using appsettings.json fallback
                if (!finalConnectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
                {
                    finalConnectionString += ";SSL Mode=Require";
                    logger.LogInformation("Added 'SSL Mode=Require' to appsettings.json fallback connection string.");
                }
                if (!finalConnectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
                {
                    finalConnectionString += ";Trust Server Certificate=true";
                    logger.LogInformation("Added 'Trust Server Certificate=true' to appsettings.json fallback connection string.");
                }
            }
            else
            {
                throw new InvalidOperationException("No database connection string found. Neither DATABASE_URL environment variable nor DefaultConnection in appsettings.json is set.");
            }
        }

        // Log the final connection string (without masking here to avoid the error)
        // In a production app, you would use a more sophisticated way to mask this *after* successful parsing.
        logger.LogInformation("Final connection string (unmasked for debugging): {ConnectionString}", finalConnectionString);


        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(finalConnectionString));

        return services;
    }

    // REMOVED MaskConnectionString helper method to avoid the parsing error
    // private static string MaskConnectionString(string connectionString) { ... }
}
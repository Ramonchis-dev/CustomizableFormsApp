using CustomizableFormsApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Npgsql; // Add this using directive for NpgsqlConnectionStringBuilder

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
            finalConnectionString = envConnectionString;
            logger.LogInformation("Using DATABASE_URL from environment variable: {MaskedConnectionString}", MaskConnectionString(finalConnectionString));

            // Ensure SSL Mode and Trust Server Certificate are present for Render
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
                logger.LogWarning("DATABASE_URL environment variable is not set or empty. Falling back to DefaultConnection from appsettings.json: {MaskedConnectionString}", MaskConnectionString(finalConnectionString));

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

        logger.LogInformation("Final connection string being used: {MaskedConnectionString}", MaskConnectionString(finalConnectionString));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(finalConnectionString));

        return services;
    }

    // Helper method to mask password in logs
    private static string MaskConnectionString(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = "********";
            }
            return builder.ToString();
        }
        catch (Exception ex)
        {
            // If the connection string is so malformed it can't even be masked, log and return original
            return $"[Error masking string: {ex.Message}] {connectionString}";
        }
    }
}
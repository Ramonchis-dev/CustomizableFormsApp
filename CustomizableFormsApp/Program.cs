using CustomizableFormsApp.Components;
using CustomizableFormsApp.Data;
using CustomizableFormsApp.Models;
using CustomizableFormsApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using CustomizableFormsApp.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using Microsoft.Extensions.Logging; // Required for ILogger

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure AuthenticationStateProvider
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
    .AddPolicy("UserPolicy", policy => policy.RequireRole("User"))
    .AddPolicy("TemplateOwner", policy =>
        policy.Requirements.Add(new OperationAuthorizationRequirement { Name = "Update" }));

builder.Services.AddSingleton<IAuthorizationHandler, TemplateOwnerHandler>();


// --- START: Database Connection String Configuration (Refactored Logging) ---
string connectionString;
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
    .CreateLogger("ProgramStartup"); // Get logger once at the top of this block

// Prioritize DATABASE_URL environment variable from Render
var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(envConnectionString))
{
    if (envConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(envConnectionString);
            var userInfo = uri.UserInfo.Split(':');

            var port = uri.Port == -1 ? 5432 : uri.Port;

            connectionString =
                $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.Trim('/')};" +
                $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
            logger.LogInformation("Successfully parsed DATABASE_URL (URI format).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse DATABASE_URL (URI format). Raw string: {RawConnectionString}", envConnectionString);
            throw new InvalidOperationException("Failed to parse DATABASE_URL environment variable as a PostgreSQL URI.", ex);
        }
    }
    else
    {
        connectionString = envConnectionString;
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
            logger.LogWarning("DATABASE_URL is not a URI. Added 'SSL Mode=Require'.");
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
            logger.LogWarning("DATABASE_URL is not a URI. Added 'Trust Server Certificate=true'.");
        }
    }
}
else
{
    var appSettingsConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(appSettingsConnectionString))
    {
        connectionString = appSettingsConnectionString;
        logger.LogWarning("DATABASE_URL environment variable is not set or empty. Falling back to DefaultConnection from appsettings.json.");

        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
            logger.LogWarning("Added 'SSL Mode=Require' to appsettings.json fallback connection string.");
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
            logger.LogWarning("Added 'Trust Server Certificate=true' to appsettings.json fallback connection string.");
        }
    }
    else
    {
        throw new InvalidOperationException("No database connection string found. Neither DATABASE_URL environment variable nor DefaultConnection in appsettings.json is set.");
    }
}

// Log the final connection string (unmasked for debugging)
logger.LogInformation("Final connection string being used (UNMASKED): {ConnectionString}", connectionString);

// --- CRITICAL: REGISTER ApplicationDbContext HERE, BEFORE Identity and other services ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// --- END CRITICAL ---

// --- END: Database Connection String Configuration ---


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>() // This depends on ApplicationDbContext
    .AddDefaultTokenProviders();


// Register your custom services (these also depend on ApplicationDbContext)
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<FormSubmissionService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

// Apply migrations and seed initial data on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.Migrate();

        await SeedData.Initialize(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        services.GetRequiredService<ILogger<Program>>().LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
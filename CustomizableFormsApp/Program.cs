using CustomizableFormsApp.Authorization;
using CustomizableFormsApp.Components;
using CustomizableFormsApp.Data;
using CustomizableFormsApp.Models;
using CustomizableFormsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

var builder = WebApplication.CreateBuilder(args);

var startupLogger = builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("ProgramStartup");


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure AuthenticationStateProvider for Blazor's cascading authentication state
builder.Services.AddCascadingAuthenticationState();

// Configure Authorization policies using the modern AddAuthorizationBuilder
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
    .AddPolicy("UserPolicy", policy => policy.RequireRole("User"))
    .AddPolicy("TemplateOwner", policy =>
        policy.Requirements.Add(new OperationAuthorizationRequirement { Name = "Update" }));

// Register custom authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, TemplateOwnerHandler>();


// --- START: Database Connection String Configuration ---
string connectionString;

// Prioritize DATABASE_URL environment variable (common for Render deployment)
var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(envConnectionString))
{
    // If DATABASE_URL is present, attempt to parse it as a postgresql:// URI
    if (envConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(envConnectionString);
            var userInfo = uri.UserInfo.Split(':');

            // Use default PostgreSQL port (5432) if not specified in the URI
            var port = uri.Port == -1 ? 5432 : uri.Port;

            connectionString =
                $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.Trim('/')};" +
                $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
            startupLogger.LogInformation("Successfully parsed DATABASE_URL (URI format).");
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex, "Failed to parse DATABASE_URL (URI format). Raw string: {RawConnectionString}", envConnectionString);
            throw new InvalidOperationException("Failed to parse DATABASE_URL environment variable as a PostgreSQL URI.", ex);
        }
    }
    else
    {
        // If DATABASE_URL is not a URI, assume it's a direct connection string.
        // Ensure SSL parameters are added as Render requires them.
        connectionString = envConnectionString;
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
            startupLogger.LogWarning("DATABASE_URL is not a URI. Added 'SSL Mode=Require'.");
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
            startupLogger.LogWarning("DATABASE_URL is not a URI. Added 'Trust Server Certificate=true'.");
        }
    }
}
else
{
    // Fallback to DefaultConnection from appsettings.json for local development
    var appSettingsConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(appSettingsConnectionString))
    {
        connectionString = appSettingsConnectionString;
        startupLogger.LogWarning("DATABASE_URL environment variable is not set or empty. Falling back to DefaultConnection from appsettings.json.");

        // Ensure SSL parameters are added if needed for local testing against Render-like setup
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
            startupLogger.LogWarning("Added 'SSL Mode=Require' to appsettings.json fallback connection string.");
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
            startupLogger.LogWarning("Added 'Trust Server Certificate=true' to appsettings.json fallback connection string.");
        }
    }
    else
    {
        throw new InvalidOperationException("No database connection string found. Neither DATABASE_URL environment variable nor DefaultConnection in appsettings.json is set.");
    }
}

// Log the final connection string (unmasked for debugging purposes)
startupLogger.LogInformation("Final connection string being used (UNMASKED): {ConnectionString}", connectionString);

// CRITICAL: Register ApplicationDbContext here. This MUST be done BEFORE Identity services
// or any other services that depend on ApplicationDbContext.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// --- END: Database Connection String Configuration ---


// Configure Authentication services, including cookie authentication for Identity
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme; // Use ExternalScheme for external logins, or ApplicationScheme if not using external
})
.AddIdentityCookies(); // Adds cookie authentication specifically for Identity


// Configure Identity services for ApplicationUser and IdentityRole
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Disable email confirmation requirement for easier development/testing
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>() // Uses ApplicationDbContext for Identity storage
    .AddDefaultTokenProviders(); // Adds default token providers for password resets, etc.


// Register your custom application services
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<FormSubmissionService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint(); // Provides UI for applying migrations in development
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true); // Global error handling for production
    app.UseHsts(); // Enforces HTTPS for security
}

app.UseHttpsRedirection(); // Redirects HTTP requests to HTTPS
app.UseStaticFiles(); // Serves static files from wwwroot
app.UseAntiforgery(); // Adds antiforgery token for form submissions

// Maps Razor Components to endpoints and enables interactive render modes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode() // Enables Server-side Blazor interactivity
    .AddInteractiveWebAssemblyRenderMode(); // Enables WebAssembly Blazor interactivity (if Client project is present)

// Apply database migrations and seed initial data on application startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Apply any pending database migrations
        context.Database.Migrate();

        // Seed initial data (e.g., admin user, roles)
        await SeedData.Initialize(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        // Log any errors that occur during migration or seeding
        services.GetRequiredService<ILogger<Program>>().LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run(); // Runs the application

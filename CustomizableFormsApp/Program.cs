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
using System; // Required for Uri
using Microsoft.Extensions.Logging; // Required for ILogger

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure AuthenticationStateProvider
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// --- START: Database Connection String Configuration ---
string connectionString;

// Prioritize DATABASE_URL environment variable from Render
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

            // --- FIX: Handle default port if not specified in URI ---
            var port = uri.Port == -1 ? 5432 : uri.Port; // Use 5432 if port is -1
            // --- END FIX ---

            connectionString =
                $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.Trim('/')};" +
                $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
            builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("Program").LogInformation("Successfully parsed DATABASE_URL (URI format).");
        }
        catch (Exception ex)
        {
            builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("Program").LogError(ex, "Failed to parse DATABASE_URL (URI format). Raw string: {RawConnectionString}", envConnectionString);
            throw new InvalidOperationException("Failed to parse DATABASE_URL environment variable as a PostgreSQL URI.", ex);
        }
    }
    else
    {
        // If DATABASE_URL is set but NOT a postgresql:// URI, assume it's a direct connection string.
        // Ensure SSL parameters are added if not already present, as Render requires them.
        connectionString = envConnectionString;
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
            builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("Program").LogWarning("DATABASE_URL is not a URI. Added 'SSL Mode=Require'.");
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
            builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("Program").LogWarning("DATABASE_URL is not a URI. Added 'Trust Server Certificate=true'.");
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
        builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
            .CreateLogger("Program").LogWarning("DATABASE_URL environment variable is not set or empty. Falling back to DefaultConnection from appsettings.json.");

        // Ensure SSL parameters are added if needed for local testing against Render-like setup
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
            builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("Program").LogWarning("Added 'SSL Mode=Require' to appsettings.json fallback connection string.");
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
            builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("Program").LogWarning("Added 'Trust Server Certificate=true' to appsettings.json fallback connection string.");
        }
    }
    else
    {
        throw new InvalidOperationException("No database connection string found. Neither DATABASE_URL environment variable nor DefaultConnection in appsettings.json is set.");
    }
}

// Log the final connection string (unmasked for debugging)
builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
    .CreateLogger("Program").LogInformation("Final connection string being used (UNMASKED): {ConnectionString}", connectionString);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// --- END: Database Connection String Configuration ---


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
    options.AddPolicy("TemplateOwner", policy =>
        policy.Requirements.Add(new OperationAuthorizationRequirement { Name = "Update" }));
});

builder.Services.AddSingleton<IAuthorizationHandler, TemplateOwnerHandler>();

// Register your custom services
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
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
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
using System;

var builder = WebApplication.CreateBuilder(args);

// var startupLogger = builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("ProgramStartup");


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

string connectionString;

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
            // No startupLogger.LogInformation here
        }
        catch (Exception ex)
        {
            // If an error occurs here, it will be an unhandled exception crashing the app
            // and you'll need to rely on Render's default error logging.
            throw new InvalidOperationException("Failed to parse DATABASE_URL environment variable as a PostgreSQL URI.", ex);
        }
    }
    else
    {
        connectionString = envConnectionString;
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
        }
    }
}
else
{
    var appSettingsConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(appSettingsConnectionString))
    {
        connectionString = appSettingsConnectionString;
        // No startupLogger.LogWarning here
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Require";
        }
        if (!connectionString.Contains("Trust Server Certificate=true", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
        }
    }
    else
    {
        throw new InvalidOperationException("No database connection string found. Neither DATABASE_URL environment variable nor DefaultConnection in appsettings.json is set.");
    }
}


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
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
        // This logger is still available because it's resolved *after* services are built
        services.GetRequiredService<ILogger<Program>>().LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
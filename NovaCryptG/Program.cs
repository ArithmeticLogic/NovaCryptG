using Microsoft.EntityFrameworkCore;
using NovaCryptG.Components;
using NovaCryptG.Data;
using NovaCryptG.Models;
using NovaCryptG.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor setup
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = false;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Scoped services
builder.Services.AddScoped<CryptographyService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserSessionService>();

// --- SQLite database setup ---
var dbFolder = Path.Combine(AppContext.BaseDirectory, "Data");
Directory.CreateDirectory(dbFolder); // ensure folder exists
var dbPath = Path.Combine(dbFolder, "LoginData.db");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Build the app
var app = builder.Build();

// Create database table if it does not exist
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();

    db.Database.EnsureCreated();

    if (!db.LoginCredentials.Any())
    {
        db.LoginCredentials.Add(new LoginCredential
        {
            UserName = "Admin",
            UserPassword = BCrypt.Net.BCrypt.HashPassword("Admin"),
            IsAdmin = true
        });
        db.SaveChanges();
    }
}

// Production settings
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map Blazor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Run the app
app.Run();
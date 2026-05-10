using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NovaCryptG.Components;
using NovaCryptG.Data;
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
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<RegistrationService>();

// SQLite database path
// TODO: Maybe add this try catch to other code that also looks for database data.
try
{
    var dbFolder = Path.Combine(AppContext.BaseDirectory, "Data");
    var dbPath = Path.Combine(dbFolder, "LoginData.db");

    builder.Services.AddDbContextFactory<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}
catch (SqliteException ex)
{
    Console.WriteLine("Sqlite connection could not be established.");
    Console.WriteLine(ex);
}

// Build the app
var app = builder.Build();

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
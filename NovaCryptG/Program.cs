using NovaCryptG.Components;
using NovaCryptG.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = false;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<CryptographyService>();
builder.Services.AddScoped<FileStorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
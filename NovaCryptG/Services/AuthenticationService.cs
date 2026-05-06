using Microsoft.EntityFrameworkCore;
using NovaCryptG.Data;
using NovaCryptG.Models;

namespace NovaCryptG.Services;

public class AuthenticationService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<LoginCredential?> LoginAsync(string username, string password)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.LoginCredentials
            .FirstOrDefaultAsync(c => c.UserName == username);

        if (user is null)
        {
            return null;
        }

        // Verify hashed password
        if (!BCrypt.Net.BCrypt.Verify(password, user.UserPassword))
        {
            return null;
        }

        return user;
    }

    public async Task<bool> IsUserAdminAsync(string username)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.LoginCredentials
            .FirstOrDefaultAsync(u => u.UserName == username);
        return user?.IsAdmin ?? false;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
    {
        // No password validation is required, due to the admin user being in control of account creation

        await using var db = await contextFactory.CreateDbContextAsync();

        // Check if username already exists (case-insensitive for safety)
        var exists = await db.LoginCredentials
            .AnyAsync(u => u.UserName.ToLower() == username.ToLower());
        if (exists)
        {
            return (false, "Username already taken.");
        }

        // Using BCrypt to hash the password
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        db.LoginCredentials.Add(new LoginCredential
        {
            UserName = username,
            UserPassword = hash
        });

        await db.SaveChangesAsync();
        return (true, "User registered successfully.");
    }
}
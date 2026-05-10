using Microsoft.EntityFrameworkCore;
using NovaCryptG.Data;
using NovaCryptG.Models;

namespace NovaCryptG.Services;

public class AuthenticationService(IDbContextFactory<AppDbContext> contextFactory)
{
    // Function to authenticate that a user's username and password combo is valid for login.
    public async Task<LoginCredential?> LoginAsync(string username, string password)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        // Verify username
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

    // Function to check if a user has admin privilege
    public async Task<bool> IsUserAdminAsync(string username)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.LoginCredentials
            .FirstOrDefaultAsync(u => u.UserName == username);
        return user?.IsAdmin ?? false;
    }
}
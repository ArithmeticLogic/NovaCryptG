using Microsoft.EntityFrameworkCore;
using NovaCryptG.Data;
using NovaCryptG.Models;

namespace NovaCryptG.Services;

public class AuthenticationService(IDbContextFactory<AppDbContext> contextFactory, ILogger<AuthenticationService> logger)
{
    // Function to authenticate that a user's username and password combo is valid for login.
    public async Task<LoginCredential?> LoginAsync(string username, string password)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        try
        {
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

            logger.LogInformation("User {Username} has logged in successfully", username);
            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error during login for user {Username}", username);
            return null; // fail-safe, treat as invalid login
        }
    }

    // Function to check if a user has admin privilege
    public async Task<bool> IsUserAdminAsync(string username)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        try
        {
            var user = await db.LoginCredentials
                .FirstOrDefaultAsync(u => u.UserName == username);
            return user?.IsAdmin ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error checking admin status for user {Username}", username);
            return false;
        }
    }
}
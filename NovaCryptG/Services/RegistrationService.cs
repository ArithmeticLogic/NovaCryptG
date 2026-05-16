using Microsoft.EntityFrameworkCore;
using NovaCryptG.Data;
using NovaCryptG.Models;

namespace NovaCryptG.Services;

// This service is not present in the designs and should not technically exist. However, I needed a better way to add users.
public class RegistrationService(IDbContextFactory<AppDbContext> contextFactory, ILogger<RegistrationService> logger)
{
    // Function to add a new user to the database, including username and password combo.
    public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
    {
        // No password validation is required, due to the admin user being in control of account creation.

        await using var db = await contextFactory.CreateDbContextAsync();

        try
        {
            // Check if username already exists (case-insensitive for safety)
            var exists = await db.LoginCredentials
                .AnyAsync(u => u.UserName.ToLower() == username.ToLower());
            if (exists)
            {
                return (false, "Username already taken.");
            }

            // Using BCrypt to hash the password
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Adding the new user credentials to the database
            db.LoginCredentials.Add(new LoginCredential
            {
                UserName = username,
                UserPassword = hash
            });

            await db.SaveChangesAsync();
            logger.LogInformation("User {Username} has been registered successfully", username.ToLower());
            return (true, "User registered successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error while registering user {Username}", username);
            return (false, "A database error occurred while creating the user.");
        }
    }
}
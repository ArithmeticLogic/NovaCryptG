namespace NovaCryptG.Models;

public class LoginCredential
{
    public string UserName { get; set; } = string.Empty; // Plain text
    public string UserPassword { get; set; } = string.Empty; // Hashed using BCrypt
    public bool IsAdmin { get; set; } // Admin user check
}
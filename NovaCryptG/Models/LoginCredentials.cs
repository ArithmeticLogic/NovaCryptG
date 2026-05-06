namespace NovaCryptG.Models;

public class LoginCredential
{
    public string UserName { get; set; } = string.Empty;
    public string UserPassword { get; set; } = string.Empty; // bcrypt hash
    public bool IsAdmin { get; set; } // Admin user check
}
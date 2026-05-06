namespace NovaCryptG.Services;

public class UserSessionService
{
    public string? CurrentUserName { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUserName);

    // Optional event so components can react to login/logout
    public event Action? OnChanged;

    public void LogIn(string userName)
    {
        CurrentUserName = userName;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChanged?.Invoke();
}
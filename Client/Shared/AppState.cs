namespace Client.Shared;

public sealed class AppState
{
    public event Action? OnChange;

    public string? DisplayName { get; private set; }

    public void SetDisplayName(string? displayName)
    {
        DisplayName = displayName;
        OnChange?.Invoke();
    }
}

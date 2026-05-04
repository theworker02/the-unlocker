namespace TheUnlocker.RegistryClient;

public sealed class AuthService
{
    public string? Token { get; private set; }
    public string? RefreshToken { get; private set; }

    public void UseToken(string token, string? refreshToken = null)
    {
        Token = token;
        RefreshToken = refreshToken;
    }

    public void Clear()
    {
        Token = null;
        RefreshToken = null;
    }
}

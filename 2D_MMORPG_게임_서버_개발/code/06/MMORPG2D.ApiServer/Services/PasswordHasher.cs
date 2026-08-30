namespace MMORPG2D.ApiServer.Services;

public class PasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string plain)
        => BCrypt.Net.BCrypt.HashPassword(plain, WorkFactor);

    public bool Verify(string plain, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(plain, hash); }
        catch { return false; }
    }
}

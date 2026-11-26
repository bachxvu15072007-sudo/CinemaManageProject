using System.Globalization;
using System.Security.Cryptography;

namespace CinemaManageProject.Helpers;

/// <summary>
/// Băm và xác thực mật khẩu một chiều bằng PBKDF2-HMAC-SHA256 kèm salt ngẫu nhiên 16 byte.
/// Định dạng lưu trữ: PBKDF2$&lt;iterations&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt; (dài ~83 ký tự).
/// </summary>
public static class PasswordHelper
{
    private const string Prefix = "PBKDF2$";
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int DefaultIterations = 100_000;

    public static bool IsHashed(string? storedPassword)
        => !string.IsNullOrEmpty(storedPassword) && storedPassword.StartsWith(Prefix, StringComparison.Ordinal);

    public static string HashPassword(string plainPassword)
    {
        ArgumentNullException.ThrowIfNull(plainPassword);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            KeySizeBytes);

        return $"{Prefix}{DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string plainPassword, string? storedPassword, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (plainPassword is null || string.IsNullOrEmpty(storedPassword))
        {
            return false;
        }

        if (!IsHashed(storedPassword))
        {
            // Tương thích dữ liệu cũ chưa băm; tự động nâng cấp lên PBKDF2 ngay khi đăng nhập đúng.
            bool legacyMatch = string.Equals(plainPassword, storedPassword, StringComparison.Ordinal);
            needsUpgrade = legacyMatch;
            return legacyMatch;
        }

        string[] parts = storedPassword.Split('$');
        if (parts.Length != 4
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations)
            || iterations < 10_000)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);
            if (salt.Length == 0 || expectedHash.Length == 0)
            {
                return false;
            }

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                plainPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            bool ok = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            needsUpgrade = ok && iterations < DefaultIterations;
            return ok;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

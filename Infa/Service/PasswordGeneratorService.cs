using System.Security.Cryptography;
using System.Text;
using Core.Interface.Service;

namespace Infa.Service;

/// <summary>
/// Implementation of password generator using cryptographically secure random number generation.
/// </summary>
public class PasswordGeneratorService : IPasswordGenerator
{
    private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string SpecialChars = "!@#$%^&*";

    /// <inheritdoc />
    public string GenerateTemporaryPassword(int length = 12, bool includeSpecialChars = true)
    {
        if (length < 8)
            throw new ArgumentException("Password length must be at least 8 characters", nameof(length));

        var charSet = UpperCase + LowerCase + Digits;
        if (includeSpecialChars)
            charSet += SpecialChars;

        var password = new StringBuilder(length);
        using var rng = RandomNumberGenerator.Create();

        // Ensure at least one of each required character type
        password.Append(GetRandomChar(UpperCase, rng));
        password.Append(GetRandomChar(LowerCase, rng));
        password.Append(GetRandomChar(Digits, rng));
        if (includeSpecialChars)
            password.Append(GetRandomChar(SpecialChars, rng));

        // Fill the rest with random characters
        var remainingLength = length - password.Length;
        for (int i = 0; i < remainingLength; i++)
        {
            password.Append(GetRandomChar(charSet, rng));
        }

        // Shuffle the password to avoid predictable patterns
        return Shuffle(password.ToString(), rng);
    }

    private static char GetRandomChar(string charSet, RandomNumberGenerator rng)
    {
        var randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        var randomIndex = BitConverter.ToUInt32(randomBytes, 0) % (uint)charSet.Length;
        return charSet[(int)randomIndex];
    }

    private static string Shuffle(string input, RandomNumberGenerator rng)
    {
        var chars = input.ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var j = (int)(BitConverter.ToUInt32(randomBytes, 0) % (uint)(i + 1));
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }
}

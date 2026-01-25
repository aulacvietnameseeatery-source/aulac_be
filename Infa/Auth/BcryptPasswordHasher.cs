using Core.Interface.Service.Auth;
using BC = BCrypt.Net.BCrypt;

namespace Infa.Auth;

/// <summary>
/// BCrypt-based implementation of IPasswordHasher.
/// Provides secure password hashing with automatic salt generation.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // 2^12 = 4096 iterations

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BC.EnhancedHashPassword(password, WorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BC.EnhancedVerify(password, hashedPassword);
    }
}

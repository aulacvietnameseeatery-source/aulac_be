namespace Core.Interface.Service;

/// <summary>
/// Password hasher abstraction for secure password verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
  /// Hashes a password using a secure algorithm.
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The stored hash</param>
    /// <returns>True if the password matches; otherwise false</returns>
    bool VerifyPassword(string password, string hashedPassword);
}

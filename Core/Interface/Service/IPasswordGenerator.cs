namespace Core.Interface.Service;

/// <summary>
/// Service for generating random secure passwords.
/// </summary>
public interface IPasswordGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random password.
    /// </summary>
    /// <param name="length">Password length (default: 12)</param>
  /// <param name="includeSpecialChars">Include special characters (default: true)</param>
    /// <returns>Random password</returns>
    string GenerateTemporaryPassword(int length = 12, bool includeSpecialChars = true);
}

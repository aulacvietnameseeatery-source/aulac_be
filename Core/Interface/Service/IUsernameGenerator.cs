namespace Core.Interface.Service;

/// <summary>
/// Service for generating unique usernames from full names.
/// </summary>
public interface IUsernameGenerator
{
    /// <summary>
    /// Generates a unique username from a full name.
    /// Handles collisions by appending incremental numbers.
    /// </summary>
    /// <param name="fullName">The user's full name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unique username (guaranteed to not exist in database)</returns>
    Task<string> GenerateUniqueUsernameAsync(
        string fullName,
        CancellationToken cancellationToken = default);
}

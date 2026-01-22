using Core.Entity;

namespace Core.Interface.Repo;

/// <summary>
/// Session repository abstraction for managing authentication sessions.
/// This interface defines the contract for session persistence operations.
/// </summary>
public interface IAuthSessionRepository
{
    /// <summary>
    /// Creates a new authentication session for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="tokenHash">The hashed refresh token (never store raw tokens)</param>
    /// <param name="expiresAt">When the session/refresh token expires</param>
    /// <param name="deviceInfo">Optional device/client information</param>
    /// <param name="ipAddress">Optional IP address of the client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created session entity</returns>
    Task<AuthSession> CreateSessionAsync(
        long userId,
        string tokenHash,
        DateTime expiresAt,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a valid (non-revoked, non-expired) session by its ID.
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The session if valid; otherwise null</returns>
    Task<AuthSession?> GetValidSessionAsync(
        long sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a session by its ID regardless of validity.
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The session if found; otherwise null</returns>
    Task<AuthSession?> GetSessionByIdAsync(
        long sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token against a session.
    /// Checks: session exists, not revoked, not expired, token hash matches.
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="tokenHash">The hashed refresh token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The session if validation succeeds; otherwise null</returns>
    Task<AuthSession?> ValidateRefreshTokenAsync(
        long sessionId,
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific session (logout single device).
    /// </summary>
    /// <param name="sessionId">The session to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session was found and revoked; otherwise false</returns>
    Task<bool> RevokeSessionAsync(
        long sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all sessions for a user (logout all devices).
    /// </summary>
    /// <param name="userId">The user whose sessions should be revoked</param>
    /// <param name="exceptSessionId">Optional session ID to exclude from revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions revoked</returns>
    Task<int> RevokeAllUserSessionsAsync(
        long userId,
        long? exceptSessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sessions for a user (for session management UI).
    /// </summary>
    /// <param name="userId">The user's identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active sessions</returns>
    Task<IEnumerable<AuthSession>> GetActiveSessionsAsync(
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a session with a new refresh token (token rotation).
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="newTokenHash">The new hashed refresh token</param>
    /// <param name="newExpiresAt">The new expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated session; null if session not found</returns>
    Task<AuthSession?> RotateRefreshTokenAsync(
        long sessionId,
        string newTokenHash,
        DateTime newExpiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired sessions from the database.
    /// Should be called periodically via a background job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default);
}

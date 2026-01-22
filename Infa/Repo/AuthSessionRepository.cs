using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// EF Core implementation of IAuthSessionRepository.
/// Handles all session persistence operations.
/// </summary>
public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly RestaurantMgmtContext _context;

    public AuthSessionRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

/// <inheritdoc />
    public async Task<AuthSession> CreateSessionAsync(
        long userId,
   string tokenHash,
   DateTime expiresAt,
   string? deviceInfo = null,
        string? ipAddress = null,
    CancellationToken cancellationToken = default)
    {
        var session = new AuthSession
     {
   UserId = userId,
TokenHash = tokenHash,
    ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
          Revoked = false,
    DeviceInfo = deviceInfo,
       IpAddress = ipAddress
        };

        _context.AuthSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return session;
    }

    /// <inheritdoc />
    public async Task<AuthSession?> GetValidSessionAsync(
 long sessionId,
 CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.AuthSessions
     .Where(s => s.SessionId == sessionId)
        .Where(s => s.Revoked != true)
         .Where(s => s.ExpiresAt > now)
     .FirstOrDefaultAsync(cancellationToken);
  }

    /// <inheritdoc />
    public async Task<AuthSession?> GetSessionByIdAsync(
 long sessionId,
        CancellationToken cancellationToken = default)
    {
   return await _context.AuthSessions
.FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthSession?> ValidateRefreshTokenAsync(
   long sessionId,
    string tokenHash,
      CancellationToken cancellationToken = default)
 {
        var now = DateTime.UtcNow;

        return await _context.AuthSessions
  .Where(s => s.SessionId == sessionId)
  .Where(s => s.TokenHash == tokenHash)
 .Where(s => s.Revoked != true)
          .Where(s => s.ExpiresAt > now)
      .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeSessionAsync(
  long sessionId,
        CancellationToken cancellationToken = default)
    {
    var session = await _context.AuthSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

    if (session == null)
            return false;

        session.Revoked = true;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

/// <inheritdoc />
    public async Task<int> RevokeAllUserSessionsAsync(
        long userId,
   long? exceptSessionId = null,
     CancellationToken cancellationToken = default)
    {
    var query = _context.AuthSessions
.Where(s => s.UserId == userId)
  .Where(s => s.Revoked != true);

   if (exceptSessionId.HasValue)
        {
       query = query.Where(s => s.SessionId != exceptSessionId.Value);
        }

      var sessions = await query.ToListAsync(cancellationToken);

    foreach (var session in sessions)
    {
       session.Revoked = true;
        }

await _context.SaveChangesAsync(cancellationToken);

        return sessions.Count;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuthSession>> GetActiveSessionsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

  return await _context.AuthSessions
    .Where(s => s.UserId == userId)
            .Where(s => s.Revoked != true)
            .Where(s => s.ExpiresAt > now)
.OrderByDescending(s => s.CreatedAt)
  .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
  public async Task<AuthSession?> RotateRefreshTokenAsync(
        long sessionId,
        string newTokenHash,
   DateTime newExpiresAt,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.AuthSessions
    .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        if (session == null || session.Revoked == true)
            return null;

     session.TokenHash = newTokenHash;
    session.ExpiresAt = newExpiresAt;

        await _context.SaveChangesAsync(cancellationToken);

  return session;
    }

    /// <inheritdoc />
 public async Task<int> CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default)
    {
    var now = DateTime.UtcNow;

 // Delete sessions that are either:
        // 1. Expired (past expiration date)
    // 2. Revoked and older than 30 days (for audit purposes)
        var cutoffDate = now.AddDays(-30);

        var expiredSessions = await _context.AuthSessions
      .Where(s => s.ExpiresAt < now ||
        s.Revoked == true && s.CreatedAt < cutoffDate)
        .ToListAsync(cancellationToken);

        _context.AuthSessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync(cancellationToken);

return expiredSessions.Count;
    }
}

using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// EF Core implementation of IAccountRepository for authentication purposes.
/// </summary>
public class AccountRepository : IAccountRepository
{
    private readonly RestaurantMgmtContext _context;

    public AccountRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<StaffAccount> CreateAsync(
     StaffAccount account,
        CancellationToken cancellationToken = default)
    {
        _context.StaffAccounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(
     string emailNormalized,
      CancellationToken cancellationToken = default)
    {
        return await _context.StaffAccounts
            .AnyAsync(a => a.Email != null && a.Email.ToUpper() == emailNormalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UsernameExistsAsync(
    string username,
        CancellationToken cancellationToken = default)
    {
  return await _context.StaffAccounts
            .AnyAsync(a => a.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAccountAsync(
    StaffAccount account,
        CancellationToken cancellationToken = default)
  {
      _context.StaffAccounts.Update(account);
     await _context.SaveChangesAsync(cancellationToken);
 }

    /// <inheritdoc />
    public async Task<StaffAccount?> FindByUsernameAsync(
   string username,
    CancellationToken cancellationToken = default)
    {
        return await _context.StaffAccounts
 .Include(a => a.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(a => a.Username == username,cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StaffAccount?> FindByIdWithRoleAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StaffAccounts
        .Include(a => a.Role)
            .ThenInclude(r => r.Permissions)
   .FirstOrDefaultAsync(a => a.AccountId == userId,cancellationToken);
    }

    /// <inheritdoc />
 public async Task<StaffAccount?> FindByIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StaffAccounts
   .FirstOrDefaultAsync(a => a.AccountId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StaffAccount?> FindByEmailAsync(
        string emailNormalized,
        CancellationToken cancellationToken = default)
    {
     // Email is stored as-is, but we search case-insensitively
        // The emailNormalized parameter is already uppercased by the caller
    return await _context.StaffAccounts.Include(s=>s.Role)
  .FirstOrDefaultAsync(a => a.Email != null && a.Email.ToUpper() == emailNormalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateLastLoginAsync(
        long userId,
        DateTime loginTime,
        CancellationToken cancellationToken = default)
    {
    var account = await _context.StaffAccounts
       .FirstOrDefaultAsync(a => a.AccountId == userId, cancellationToken);

      if (account != null)
     {
    account.LastLoginAt = loginTime;
      await _context.SaveChangesAsync(cancellationToken);
     }
    }

    /// <inheritdoc />
  public async Task UpdatePasswordAsync(
        long userId,
        string newPasswordHash,
        CancellationToken cancellationToken = default)
    {
  var account = await _context.StaffAccounts
  .FirstOrDefaultAsync(a => a.AccountId == userId, cancellationToken);

 if (account != null)
        {
     account.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

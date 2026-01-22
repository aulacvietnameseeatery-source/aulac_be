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
    public async Task<Account?> FindByUsernameAsync(
   string username,
    CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(a => a.Username == username,cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Account?> FindByIdWithRoleAsync(
   long userId,
    CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(a => a.AccountId == userId,cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateLastLoginAsync(
        long userId,
        DateTime loginTime,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
       .FirstOrDefaultAsync(a => a.AccountId == userId, cancellationToken);

        if (account != null)
        {
            account.LastLoginAt = loginTime;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

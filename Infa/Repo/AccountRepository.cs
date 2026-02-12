using Core.Data;
using Core.DTO.Account;
using Core.DTO.General;
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
    return await _context.StaffAccounts.Include(s => s.Role)
            .ThenInclude(r => r.Permissions)
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

    public async Task<PagedResultDTO<AccountListDTO>> GetAccountsAsync(AccountListQueryDTO query, CancellationToken cancellationToken = default)
    {
        var queryable = _context.StaffAccounts
          .Include(a => a.Role)
          .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim().ToLower();
            queryable = queryable.Where(a =>
                a.FullName.ToLower().Contains(searchTerm) ||
                (a.Email != null && a.Email.ToLower().Contains(searchTerm)) ||
                (a.Phone != null && a.Phone.Contains(searchTerm))
            );
        }

        if (query.RoleId.HasValue)
        {
            queryable = queryable.Where(a => a.RoleId == query.RoleId.Value);
        }

        if (query.AccountStatus.HasValue)
        {
            queryable = queryable.Where(a => a.AccountStatusLvId == query.AccountStatus.Value);
        }


        var totalCount = await queryable.CountAsync(cancellationToken);

        var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Apply pagination
        var employees = await queryable
            .OrderBy(e => e.AccountId)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccountListDTO
            {
                AccountId = x.AccountId,
                FullName = x.FullName,
                Phone = x.Phone,
                Email = x.Email,
                RoleId = x.RoleId,
                RoleName = x.Role.RoleName,
                AccountStatus = (int)x.AccountStatusLvId,
                AccountStatusName = x.AccountStatusLvId == 1 ? "Active" : x.AccountStatusLvId == 2 ? "Inactive" : "Locked"
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDTO<AccountListDTO>
        {
            PageData = employees,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
    public async Task<List<RoleDTO>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Select(r => new RoleDTO
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AccountStatusDTO>> GetAccountStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LookupValues
            .Where(lv => lv.TypeId == LookupTypeInfo.AccountStatus.TypeId 
                && lv.IsActive == true
                && lv.DeletedAt == null)
            .Select(lv => new AccountStatusDTO
            {
                ValueId = lv.ValueId,
                ValueName = lv.ValueName
            })
            .OrderBy(lv => lv.ValueId)
            .ToListAsync(cancellationToken);
    }}

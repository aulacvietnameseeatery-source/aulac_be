using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

/// <summary>
/// EF Core implementation of IRoleRepository.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly RestaurantMgmtContext _context;

    public RoleRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Role?> FindByIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> FindByCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleCode == roleCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Role> Roles, int TotalCount)> GetPagedWithStaffCountAsync(int pageIndex, int pageSize, string? search)
    {
        var query = _context.Roles
                .AsNoTracking()
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.RoleCode.Contains(search) ||
                r.RoleName.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var roles = await query
            .Include(r => r.StaffAccounts)
            .OrderBy(r => r.RoleCode)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (roles, totalCount);
    }
}

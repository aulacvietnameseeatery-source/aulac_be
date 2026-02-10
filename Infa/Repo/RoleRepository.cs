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

        // Filter by ACTIVE status
        // We need to know what ID is "ACTIVE".
        // Since we don't want to hardcode 130, and we want to avoid extra DB call every time, 
        // ideally we should join with LookupValue or use a known constant if possible.
        // But per request "not hardcoded", let's look it up or join.
        // Joining in EF Core:
        query = query.Where(r => r.RoleStatusLv != null && r.RoleStatusLv.ValueCode == RoleStatusCode.ACTIVE.ToString());

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

    public async Task DeleteAsync(Role role)
    {
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
    }

    public async Task<uint?> GetRoleStatusIdAsync(string statusCode)
    {
        var status = await _context.LookupValues
            .AsNoTracking()
            .FirstOrDefaultAsync(lv => lv.Type.TypeCode == "ROLE_STATUS" && lv.ValueCode == statusCode);
        return status?.ValueId;
    }

    public async Task<bool> HasStaffAssignedAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffAccounts
            .AnyAsync(s => s.RoleId == roleId, cancellationToken);
    }
}

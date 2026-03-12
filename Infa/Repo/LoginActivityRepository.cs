using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class LoginActivityRepository : ILoginActivityRepository
{
    private readonly RestaurantMgmtContext _context;

    public LoginActivityRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public void Add(LoginActivity entity) => _context.LoginActivities.Add(entity);

    public async Task<List<LoginActivity>> GetByStaffIdAsync(long staffId, int limit = 50, CancellationToken ct = default)
    {
        return await _context.LoginActivities
            .AsNoTracking()
            .Where(la => la.StaffId == staffId)
            .OrderByDescending(la => la.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}

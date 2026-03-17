using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Repo;

public class ShiftTemplateRepository : IShiftTemplateRepository
{
    private readonly RestaurantMgmtContext _context;

    public ShiftTemplateRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public void Add(ShiftTemplate entity) => _context.ShiftTemplates.Add(entity);

    public async Task<ShiftTemplate?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _context.ShiftTemplates
            .Include(t => t.CreatedByStaff)
            .Include(t => t.UpdatedByStaff)
            .FirstOrDefaultAsync(t => t.ShiftTemplateId == id, ct);
    }

    public async Task<List<ShiftTemplate>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.ShiftTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DefaultStartTime)
                .ThenBy(t => t.TemplateName)
            .ToListAsync(ct);
    }

    public async Task<List<ShiftTemplate>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ShiftTemplates
            .AsNoTracking()
            .OrderBy(t => t.DefaultStartTime)
                .ThenBy(t => t.TemplateName)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string templateName, long? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.ShiftTemplates
            .AsNoTracking()
            .Where(t => t.TemplateName == templateName);

        if (excludeId.HasValue)
            query = query.Where(t => t.ShiftTemplateId != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> HasActiveAssignmentsAsync(long templateId, CancellationToken ct = default)
    {
        return await _context.ShiftAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ShiftTemplateId == templateId && a.IsActive, ct);
    }
}

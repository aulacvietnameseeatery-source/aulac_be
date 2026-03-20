using Core.Entity;
using Core.Interface.Repo;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infa.Repo;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly RestaurantMgmtContext _context;

    public EmailTemplateRepository(RestaurantMgmtContext context)
    {
        _context = context;
    }

    public async Task<EmailTemplate?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _context.EmailTemplates.FindAsync(new object[] { id }, ct);
    }

    public async Task<EmailTemplate?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.EmailTemplates.FirstOrDefaultAsync(x => x.TemplateCode == code, ct);
    }

    public async Task<List<EmailTemplate>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.EmailTemplates.ToListAsync(ct);
    }

    public async Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        await _context.EmailTemplates.AddAsync(template, ct);
        await _context.SaveChangesAsync(ct);
        return template;
    }

    public async Task<bool> UpdateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        _context.EmailTemplates.Update(template);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var template = await GetByIdAsync(id, ct);
        if (template == null) return false;

        _context.EmailTemplates.Remove(template);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}

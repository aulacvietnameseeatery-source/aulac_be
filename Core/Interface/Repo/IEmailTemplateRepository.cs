using Core.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interface.Repo;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<EmailTemplate?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<EmailTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<EmailTemplate> CreateAsync(EmailTemplate template, CancellationToken ct = default);
    Task<bool> UpdateAsync(EmailTemplate template, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}

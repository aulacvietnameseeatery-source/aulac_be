using Core.DTO.EmailTemplate;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interface.Service;

public interface IEmailTemplateService
{
    Task<EmailTemplateDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<EmailTemplateDto>> GetAllAsync(CancellationToken ct = default);
    Task<EmailTemplateDto> CreateAsync(CreateEmailTemplateRequest request, CancellationToken ct = default);
    Task<EmailTemplateDto> UpdateAsync(long id, UpdateEmailTemplateRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}

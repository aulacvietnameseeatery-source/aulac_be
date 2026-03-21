using Core.DTO.EmailTemplate;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Service;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _repository;

    public EmailTemplateService(IEmailTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<EmailTemplateDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var template = await _repository.GetByCodeAsync(code, ct);
        return template != null ? MapToDto(template) : null;
    }

    public async Task<List<EmailTemplateDto>> GetAllAsync(CancellationToken ct = default)
    {
        var templates = await _repository.GetAllAsync(ct);
        return templates.Select(MapToDto).ToList();
    }

    public async Task<EmailTemplateDto> CreateAsync(CreateEmailTemplateRequest request, CancellationToken ct = default)
    {
        var template = new EmailTemplate
        {
            TemplateCode = request.TemplateCode,
            TemplateName = request.TemplateName,
            Subject = request.Subject,
            BodyHtml = request.BodyHtml,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(template, ct);
        return MapToDto(created);
    }

    public async Task<EmailTemplateDto> UpdateAsync(long id, UpdateEmailTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _repository.GetByIdAsync(id, ct);
        if (template == null) throw new KeyNotFoundException($"Template ID {id} not found.");

        template.TemplateName = request.TemplateName;
        template.Subject = request.Subject;
        template.BodyHtml = request.BodyHtml;
        template.Description = request.Description;
        template.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(template, ct);
        return MapToDto(template);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        return await _repository.DeleteAsync(id, ct);
    }

    private static EmailTemplateDto MapToDto(EmailTemplate template)
    {
        return new EmailTemplateDto
        {
            TemplateId = template.TemplateId,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            Subject = template.Subject,
            BodyHtml = template.BodyHtml,
            Description = template.Description,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}

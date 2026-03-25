using Core.DTO.EmailTemplate;
using Core.Interface.Service;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailTemplateController : ControllerBase
{
    private readonly IEmailTemplateService _service;

    public EmailTemplateController(IEmailTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmailTemplateDto>>> GetAll(CancellationToken ct)
    {
        var templates = await _service.GetAllAsync(ct);
        return Ok(templates);
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<EmailTemplateDto>> GetByCode(string code, CancellationToken ct)
    {
        var template = await _service.GetByCodeAsync(code, ct);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<EmailTemplateDto>> Create(CreateEmailTemplateRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByCode), new { code = created.TemplateCode }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmailTemplateDto>> Update(long id, UpdateEmailTemplateRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

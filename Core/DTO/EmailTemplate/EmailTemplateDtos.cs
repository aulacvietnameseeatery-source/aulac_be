using System;

namespace Core.DTO.EmailTemplate;

public class EmailTemplateDto
{
    public long TemplateId { get; set; }
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string BodyHtml { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateEmailTemplateRequest
{
    public string TemplateName { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string BodyHtml { get; set; } = null!;
    public string? Description { get; set; }
}

public class CreateEmailTemplateRequest
{
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string BodyHtml { get; set; } = null!;
    public string? Description { get; set; }
}

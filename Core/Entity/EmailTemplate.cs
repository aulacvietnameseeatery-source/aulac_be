using System;

namespace Core.Entity;

public class EmailTemplate
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

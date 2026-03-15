using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Shift;

public class GetShiftTemplateRequest
{
    public bool? IsActive { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class CreateShiftTemplateRequest
{
    [Required]
    [MaxLength(100)]
    public string TemplateName { get; set; } = null!;

    [Required]
    public TimeOnly DefaultStartTime { get; set; }

    [Required]
    public TimeOnly DefaultEndTime { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateShiftTemplateRequest
{
    [MaxLength(100)]
    public string? TemplateName { get; set; }

    public TimeOnly? DefaultStartTime { get; set; }

    public TimeOnly? DefaultEndTime { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}

namespace Core.DTO.Shift;

public class ShiftTemplateListDto
{
    public long ShiftTemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public TimeOnly DefaultStartTime { get; set; }
    public TimeOnly DefaultEndTime { get; set; }
    public string? Description { get; set; }
    public int? BufferBeforeMinutes { get; set; }
    public int? BufferAfterMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ShiftTemplateDetailDto : ShiftTemplateListDto
{
    public string CreatedByName { get; set; } = string.Empty;
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

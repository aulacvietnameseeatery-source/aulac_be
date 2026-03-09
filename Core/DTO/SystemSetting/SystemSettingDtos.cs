namespace Core.DTO.SystemSetting;

/// <summary>
/// DTO for retrieving a system setting.
/// </summary>
public record SystemSettingDto
{
    public string SettingKey { get; init; } = string.Empty;
    public string ValueType { get; init; } = string.Empty;
    public object? Value { get; init; }
    public string? Description { get; init; }
    public bool IsSensitive { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating or updating a system setting.
/// </summary>
public record SaveSystemSettingDto
{
    public string SettingKey { get; init; } = string.Empty;
    public string ValueType { get; init; } = string.Empty;
    public string? ValueString { get; init; }
    public long? ValueInt { get; init; }
    public decimal? ValueDecimal { get; init; }
    public bool? ValueBool { get; init; }
    public string? ValueJson { get; init; }
    public string? Description { get; init; }
    public bool IsSensitive { get; init; }
}

/// <summary>
/// DTO for updating a setting value (simplified).
/// </summary>
public record UpdateSettingValueDto
{
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>
/// DTO for creating a new system setting.
/// </summary>
public record CreateSystemSettingDto
{
    public string Key { get; init; } = string.Empty;
    public string? SettingName { get; init; }
    public string ValueType { get; init; } = "STRING";
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSensitive { get; init; }
}

/// <summary>
/// Detailed DTO for a single system setting, including its group.
/// </summary>
public record SystemSettingDetailDto
{
    public string SettingKey { get; init; } = string.Empty;
    public string? SettingName { get; init; }
    public string Group { get; init; } = string.Empty;
    public string ValueType { get; init; } = string.Empty;
    public object? Value { get; init; }
    public string? Description { get; init; }
    public bool IsSensitive { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO representing all settings belonging to a named group.
/// </summary>
public record SystemSettingGroupDto
{
    public string GroupName { get; init; } = string.Empty;
    public List<SystemSettingDetailDto> Settings { get; init; } = new();
}

/// <summary>
/// A single item in a bulk update request.
/// </summary>
public record BulkUpdateSettingItemDto
{
    public string Key { get; init; } = string.Empty;
    public string? SettingName { get; init; }
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>
/// DTO for bulk-updating all settings in a group.
/// </summary>
public record BulkUpdateGroupDto
{
    public List<BulkUpdateSettingItemDto> Items { get; init; } = new();
}

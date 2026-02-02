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

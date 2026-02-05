namespace Core.DTO.Account;

/// <summary>
/// Response DTO for account creation.
/// </summary>
public record CreateAccountResult
{
    public long AccountId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string AccountStatus { get; init; } = string.Empty; // "LOCKED"
    public bool TemporaryPasswordSent { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Detailed account information response.
/// </summary>
public record AccountDetailDto
{
    public long AccountId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string AccountStatus { get; init; } = string.Empty; // Resolved from lookup
    public bool IsLocked { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public RoleDto? Role { get; init; }
}

/// <summary>
/// Role information.
/// </summary>
public record RoleDto
{
    public long RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
}

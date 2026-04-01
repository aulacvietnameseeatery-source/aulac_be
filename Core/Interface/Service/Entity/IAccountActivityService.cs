using Core.DTO.Account;
using Core.DTO.General;

namespace Core.Interface.Service.Entity;

/// <summary>
/// Service for fetching account-scoped activity data (orders, audit logs, etc.).
/// Used by the account detail tabs in the admin panel.
/// </summary>
public interface IAccountActivityService
{
    /// <summary>
    /// Gets paginated orders handled by a specific staff member.
    /// </summary>
    Task<PagedResultDTO<AccountOrderSummaryDTO>> GetAccountOrdersAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets paginated audit log entries for a specific staff member.
    /// </summary>
    Task<PagedResultDTO<AccountAuditLogDTO>> GetAccountAuditLogsAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets paginated login activity for a specific staff member.
    /// </summary>
    Task<PagedResultDTO<AccountLoginActivityDTO>> GetAccountLoginActivityAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets paginated service errors attributed to a specific staff member.
    /// </summary>
    Task<PagedResultDTO<AccountServiceErrorDTO>> GetAccountServiceErrorsAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets paginated inventory transactions created or approved by a specific staff member.
    /// </summary>
    Task<PagedResultDTO<AccountInventoryActivityDTO>> GetAccountInventoryActivityAsync(
        long accountId,
        AccountSubResourceQueryDTO query,
        CancellationToken ct = default);
}

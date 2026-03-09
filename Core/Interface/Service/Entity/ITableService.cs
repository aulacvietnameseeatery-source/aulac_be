using Core.DTO.LookUpValue;
using Core.DTO.Table;


namespace Core.Interface.Service.Entity;

public interface ITableService
{
// ── List / Select ─────────────────────────────────────────────────

    /// <summary>
    /// Gets a paged list of tables for the management screen, including images.
    /// </summary>
    Task<(List<TableManagementDto> Items, int TotalCount)> GetTablesForManagementAsync(
        GetTableManagementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a lightweight table list for dropdown / order-creation usage.
    /// </summary>
    Task<List<TableSelectDto>> GetTablesForSelectAsync(CancellationToken ct = default);

    // ── Single table detail ───────────────────────────────────────────

    /// <summary>
    /// Gets full detail for a table including media, active orders, and upcoming reservations.
    /// </summary>
    Task<TableDetailDto> GetTableByIdAsync(long id, CancellationToken ct = default);

    // ── Table CRUD ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new table. Optionally uploads images in the same request.
    /// </summary>
    Task<TableDetailDto> CreateTableAsync(CreateTableFormRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates a table. Optionally adds new images and/or removes existing ones in the same request.
    /// </summary>
    Task<TableDetailDto> UpdateTableAsync(long id, UpdateTableFormRequest request, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a table. Returns 409 if the table has active orders or upcoming reservations.
    /// </summary>
    Task DeleteTableAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Transitions a table to a new status. Returns 422 for invalid transitions.
    /// </summary>
    Task<TableManagementDto> UpdateStatusAsync(long id, UpdateTableStatusRequest request, CancellationToken ct = default);

    // ── Zone & Type lookups ───────────────────────────────────────────

    /// <summary>Gets all active zone lookup values.</summary>
    Task<List<LookupValueI18nDto>> GetZonesAsync(CancellationToken ct = default);

    /// <summary>Gets all active table type lookup values.</summary>
    Task<List<LookupValueI18nDto>> GetTableTypesAsync(CancellationToken ct = default);

    /// <summary>Creates a new zone lookup value.</summary>
    Task<LookupValueI18nDto> CreateZoneAsync(CreateLookupValueRequest request, CancellationToken ct = default);

    /// <summary>Creates a new table type lookup value.</summary>
    Task<LookupValueI18nDto> CreateTableTypeAsync(CreateLookupValueRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing zone lookup value.</summary>
    Task<LookupValueI18nDto> UpdateZoneAsync(uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default);

    /// <summary>Soft-deletes a zone lookup value. Returns 409 if in use.</summary>
    Task DeleteZoneAsync(uint valueId, CancellationToken ct = default);

    /// <summary>Updates an existing table type lookup value.</summary>
    Task<LookupValueI18nDto> UpdateTableTypeAsync(uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default);

    /// <summary>Soft-deletes a table type lookup value. Returns 409 if in use.</summary>
    Task DeleteTableTypeAsync(uint valueId, CancellationToken ct = default);

    // ── Bulk online toggle ────────────────────────────────────────────

    /// <summary>
    /// Bulk-toggles online/offline for all tables in a zone.
    /// Returns the number of affected tables.
    /// </summary>
    Task<int> BulkSetOnlineAsync(BulkOnlineRequest request, CancellationToken ct = default);

    // ── QR code ───────────────────────────────────────────────────────

    /// <summary>Regenerates the QR code token for a table.</summary>
    Task<QrCodeDto> RegenerateQrCodeAsync(long tableId, CancellationToken ct = default);

    // ── Table media (incremental) ─────────────────────────────────────

    /// <summary>
    /// Adds images to an existing table without changing other data.
    /// </summary>
    Task<List<TableMediaDto>> UploadTableMediaAsync(long tableId, List<MediaFileInput> files, CancellationToken ct = default);

    /// <summary>
    /// Removes a specific image from a table.
    /// </summary>
    Task DeleteTableMediaAsync(long tableId, long mediaId, CancellationToken ct = default);

    /// <summary>
        /// Marks a table as occupied by table code. Used by customers via QR code.
        /// </summary>
    Task OccupyTableByCodeAsync(string tableCode, CancellationToken ct = default);
}

/// <summary>
/// Represents a file to be uploaded for table media operations.
/// </summary>
public class MediaFileInput
{
    public Stream Stream { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}
using Core.DTO.LookUpValue;
using Core.DTO.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Table
{
    public interface ITableService
    {
        Task<(List<TableManagementDto> Items, int TotalCount)> GetTablesForManagementAsync(GetTableManagementRequest request, CancellationToken ct = default);

        Task<List<TableSelectDto>> GetTablesForSelectAsync(CancellationToken ct = default);

        Task<TableDetailDto> GetTableByIdAsync(long id, CancellationToken ct = default);

        Task<TableManagementDto> CreateTableAsync(CreateTableRequest request, CancellationToken ct = default);

        Task<TableManagementDto> UpdateTableAsync(long id, UpdateTableRequest request, CancellationToken ct = default);

        Task DeleteTableAsync(long id, CancellationToken ct = default);

        Task<TableManagementDto> UpdateStatusAsync(long id, UpdateTableStatusRequest request, CancellationToken ct = default);

        /// <summary>
        /// Gets all active zone lookup values.
        /// </summary>
        Task<List<LookupValueI18nDto>> GetZonesAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets all active table type lookup values.
        /// </summary>
        Task<List<LookupValueI18nDto>> GetTableTypesAsync(CancellationToken ct = default);


        /// <summary>
        /// Creates a new zone lookup value.
        /// </summary>
        Task<LookupValueI18nDto> CreateZoneAsync(CreateLookupValueRequest request, CancellationToken ct = default);

        /// <summary>
        /// Creates a new table type lookup value.
        /// </summary>
        Task<LookupValueI18nDto> CreateTableTypeAsync(CreateLookupValueRequest request, CancellationToken ct = default);


        /// <summary>
        /// Bulk-toggles online/offline for all tables in a zone.
        /// Returns the number of affected tables.
        /// </summary>
        Task<int> BulkSetOnlineAsync(BulkOnlineRequest request, CancellationToken ct = default);


        /// <summary>
        /// Regenerates the QR code for a table from its current table code.
        /// </summary>
        Task<QrCodeDto> RegenerateQrCodeAsync(long tableId, CancellationToken ct = default);

        /// <summary>
        /// Uploads images for a table and returns the created media records.
        /// </summary>
        Task<List<TableMediaDto>> UploadTableMediaAsync(long tableId, List<MediaFileInput> files, CancellationToken ct = default);

        /// <summary>
        /// Deletes a specific media image from a table.
        /// </summary>
        Task DeleteTableMediaAsync(long tableId, long mediaId, CancellationToken ct = default);


        /// <summary>
        /// Updates an existing zone lookup value.
        /// </summary>
        Task<LookupValueI18nDto> UpdateZoneAsync(uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default);

        /// <summary>
        /// Soft-deletes a zone lookup value. Returns 409 if in use.
        /// </summary>
        Task DeleteZoneAsync(uint valueId, CancellationToken ct = default);


        /// <summary>
        /// Updates an existing table type lookup value.
        /// </summary>
        Task<LookupValueI18nDto> UpdateTableTypeAsync(uint valueId, UpdateLookupValueRequest request, CancellationToken ct = default);

        /// <summary>
        /// Soft-deletes a table type lookup value. Returns 409 if in use.
        /// </summary>
        Task DeleteTableTypeAsync(uint valueId, CancellationToken ct = default);

        /// <summary>
        /// Marks a table as occupied by table code. Used by customers via QR code.
        /// </summary>
        Task OccupyTableByCodeAsync(string tableCode, CancellationToken ct = default);
    }

    /// <summary>
    /// Represents an uploaded file for table media operations.
    /// </summary>
    public class MediaFileInput
    {
        public Stream Stream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
    }
}

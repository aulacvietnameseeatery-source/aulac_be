using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Core.DTO.Inventory;

/// <summary>
/// Multipart/form-data wrapper for creating an inventory transaction with evidence files.
/// The actual request payload is JSON-serialized inside <see cref="RequestJson"/>.
/// </summary>
public class CreateInventoryTransactionFormRequest
{
    /// <summary>
    /// JSON-serialized <see cref="CreateInventoryTransactionRequest"/>.
    /// </summary>
    [Required]
    public string RequestJson { get; set; } = string.Empty;

    /// <summary>
    /// Optional evidence image files (max 5, 5 MB each).
    /// </summary>
    public List<IFormFile>? EvidenceFiles { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Inventory;

/// <summary>
/// A single line item in a transaction request.
/// </summary>
public class TransactionItemRequest
{
    [Required]
    public long IngredientId { get; set; }

    /// <summary>
    /// Quantity for IN/OUT. For stock-check (ADJUST), this is the actual counted quantity.
    /// </summary>
    [Required]
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    [Required]
    public uint UnitLvId { get; set; }

    /// <summary>
    /// Unit cost per item, mainly for IN (import) transactions.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }
}

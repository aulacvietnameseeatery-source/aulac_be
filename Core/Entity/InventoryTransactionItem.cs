using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class InventoryTransactionItem
{
    public long TransactionItemId { get; set; }

    public long TransactionId { get; set; }

    public long IngredientId { get; set; }

    public decimal Quantity { get; set; }

    // _OLD: public string Unit { get; set; } = null!;
    /// <summary>
    /// FK → LookupValue (INGREDIENT_UNIT): kg, liter, piece, etc.
    /// </summary>
    public uint UnitLvId { get; set; }

    /// <summary>
    /// Unit price for IN (import) transactions
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Stock-check only: system quantity before counting
    /// </summary>
    public decimal? SystemQuantity { get; set; }

    /// <summary>
    /// Stock-check only: actual counted quantity (Quantity field stores the variance)
    /// </summary>
    public decimal? ActualQuantity { get; set; }

    /// <summary>
    /// FK → LookupValue (VARIANCE_REASON): BREAKAGE, NATURAL_LOSS, COUNTING_ERROR
    /// Stock-check only.
    /// </summary>
    public uint? VarianceReasonLvId { get; set; }

    public string? Note { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual InventoryTransaction Transaction { get; set; } = null!;

    public virtual LookupValue UnitLv { get; set; } = null!;

    public virtual LookupValue? VarianceReasonLv { get; set; }
}

using System;

namespace Core.DTO.Tax;

public class TaxDTO
{
    public long TaxId { get; set; }
    public string TaxName { get; set; } = null!;
    public decimal TaxRate { get; set; }
    public string TaxType { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTaxRequestDTO
{
    public string TaxName { get; set; } = null!;
    public decimal TaxRate { get; set; }
    public string TaxType { get; set; } = "EXCLUSIVE";
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}

public class UpdateTaxRequestDTO
{
    public string? TaxName { get; set; }
    public decimal? TaxRate { get; set; }
    public string? TaxType { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
}

using System;
using System.Collections.Generic;

namespace Core.DTO.Order
{
    public class SaleInvoiceDTO
    {
        public long OrderId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        
        // Restaurant Info (Typically fetched from System Settings, but we can return placeholders or fetch if available)
        public string RestaurantName { get; set; } = string.Empty;
        public string RestaurantAddress { get; set; } = string.Empty;
        public string RestaurantPhone { get; set; } = string.Empty;

        // Order Info
        public string OrderType { get; set; } = string.Empty; // e.g. DINE_IN, TAKEAWAY
        public string TableCode { get; set; } = string.Empty;
        
        // Staff Info
        public string StaffName { get; set; } = string.Empty;

        // Customer Info
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Pricing Info
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; } // Total = SubTotal - DiscountAmount

        public bool IsPaid { get; set; }

        public List<SaleInvoiceItemDTO> Items { get; set; } = new();
    }

    public class SaleInvoiceItemDTO
    {
        public long OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal ItemPrice { get; set; }
        public decimal Amount { get; set; } // ItemPrice * Quantity
        public string? Note { get; set; }
    }
}

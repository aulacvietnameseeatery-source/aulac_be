using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Order
{
    public class OrderDetailDTO
    {
        public long OrderId { get; set; }

        public long? TableId { get; set; }
        public string TableCode { get; set; } = "";

        public long? StaffId { get; set; }
        public string StaffName { get; set; } = "";

        public long CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public decimal SubTotalAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public long? TaxId { get; set; }
        public decimal? TipAmount { get; set; }

        public string OrderStatus { get; set; } = "";
        public string Source { get; set; } = "";

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsPaid { get; set; }

        public List<OrderItemDTO> OrderItems { get; set; } = new();

        public List<OrderPromotionDTO> Promotions { get; set; } = new();

        public List<OrderCouponDTO> Coupons { get; set; } = new();

        public List<OrderPaymentDTO> Payments { get; set; } = new();

        public int ItemCount => OrderItems.Count;
    }

    public class OrderPromotionDTO
    {
        public long PromotionId { get; set; }
        public string PromotionName { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
    }

    public class OrderCouponDTO
    {
        public long CouponId { get; set; }
        public string CouponCode { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
    }

    public class OrderPaymentDTO
    {
        public long PaymentId { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Method { get; set; } = null!;
    }
}

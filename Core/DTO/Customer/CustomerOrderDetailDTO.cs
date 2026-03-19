using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Customer
{
    public class CustomerOrderDetailDTO
    {
        public long OrderId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal? TipAmount { get; set; }

        public string Status { get; set; } = null!;

        public string OrderType { get; set; } = null!;

        public string? TableCode { get; set; }

        public string? StaffName { get; set; }

        public List<CustomerOrderItemDTO> Items { get; set; } = new();

        public List<CustomerOrderPromotionDTO> Promotions { get; set; } = new();

        public List<CustomerOrderCouponDTO> Coupons { get; set; } = new();

        public List<CustomerOrderPaymentDTO> Payments { get; set; } = new();
    }

    public class CustomerOrderItemDTO
    {
        public long OrderItemId { get; set; }

        public long DishId { get; set; }

        public string DishName { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public string Status { get; set; } = null!;

        public string? Note { get; set; }
    }

    public class CustomerOrderPromotionDTO
    {
        public long PromotionId { get; set; }

        public string PromotionName { get; set; } = null!;

        public decimal DiscountAmount { get; set; }
    }

    public class CustomerOrderCouponDTO
    {
        public long CouponId { get; set; }

        public string CouponCode { get; set; } = null!;

        public decimal DiscountAmount { get; set; }
    }

    public class CustomerOrderPaymentDTO
    {
        public long PaymentId { get; set; }

        public decimal ReceivedAmount { get; set; }

        public decimal ChangeAmount { get; set; }

        public DateTime? PaidAt { get; set; }

        public string Method { get; set; } = null!;
    }
}

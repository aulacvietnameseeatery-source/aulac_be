using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Promotion
{
    public class PromotionAvailableDTO
    {
        public long PromotionId { get; set; }

        public string PromoCode { get; set; } = null!;

        public string PromoName { get; set; } = null!;

        public string PromotionType { get; set; } = null!;

        public decimal DiscountValue { get; set; }

        public decimal EstimatedDiscount { get; set; }

        public decimal FinalAmount { get; set; }
    }
}

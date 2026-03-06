using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Promotion
{
    public class PromotionListDTO
    {
        public long PromotionId { get; set; }

        public string PromoCode { get; set; } = "";
        public string PromoName { get; set; } = "";

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public decimal DiscountValue { get; set; }

        public int? MaxUsage { get; set; }
        public int? UsedCount { get; set; }

        public string Type { get; set; } = "";

        public string PromotionStatus { get; set; } = "";
    }

    public class PromotionListQueryDTO
    {
        public string? Search { get; set; }

        public string? PromotionStatus { get; set; }

        public string? Type { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}

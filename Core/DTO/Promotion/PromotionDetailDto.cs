using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.DTO.Promotion
{
    public class PromotionDetailDto
    {
        public long PromotionId { get; set; }

        public string PromoCode { get; set; } = null!;
        public string PromoName { get; set; } = null!;
        public string? Description { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PromotionTypeCode Type { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PromotionStatusCode PromotionStatus { get; set; }

        public decimal DiscountValue { get; set; }

        public int? MaxUsage { get; set; }
        public int? UsedCount { get; set; }

        public List<PromotionRuleDto> PromotionRules { get; set; } = new();
        public List<PromotionTargetDto> PromotionTargets { get; set; } = new();
    }
}

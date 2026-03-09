using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.DTO.Promotion
{
    public class PromotionRuleDto
    {
        public long? RuleId { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int? MinQuantity { get; set; }
        public long? RequiredDishId { get; set; }
        public long? RequiredCategoryId { get; set; }
    }

    public class PromotionTargetDto
    {
        public long? TargetId { get; set; }
        public long? DishId { get; set; }
        public long? CategoryId { get; set; }
    }

    public class PromotionDto
    {
        public long? PromotionId { get; set; }

        public string PromoCode { get; set; } = null!;
        public string PromoName { get; set; } = null!;
        public string? Description { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PromotionTypeCode Type { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PromotionStatusCode? PromotionStatus { get; set; }

        public decimal DiscountValue { get; set; }

        public int? MaxUsage { get; set; }

        public List<PromotionRuleDto> PromotionRules { get; set; } = new();
        public List<PromotionTargetDto> PromotionTargets { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;

namespace Core.Entity;

public partial class Promotion
{
    public long PromotionId { get; set; }

    public string? PromoCode { get; set; }

    public string PromoName { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// PromotionType (numeric enum in app)
    /// </summary>
    public byte TypeId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    /// <summary>
    /// PromotionStatus: 1=SCHEDULED,2=ACTIVE,3=EXPIRED,4=DISABLED
    /// </summary>
    public byte PromotionStatus { get; set; }

    public int? MaxUsage { get; set; }

    public int? UsedCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public uint TypeLvId { get; set; }

    public uint PromotionStatusLvId { get; set; }

    public virtual ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();

    public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();

    public virtual LookupValue PromotionStatusLv { get; set; } = null!;

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();

    public virtual LookupValue TypeLv { get; set; } = null!;
}

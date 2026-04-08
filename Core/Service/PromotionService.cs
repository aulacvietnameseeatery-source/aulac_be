using Core.DTO.General;
using Core.DTO.Promotion;
using Core.Entity;
using Core.Enum;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Promotion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILookupResolver _lookupResolver;

        public PromotionService(
            IPromotionRepository promotionRepository,
            IOrderRepository orderRepository,
            ILookupResolver lookupResolver)
        {
            _promotionRepository = promotionRepository;
            _orderRepository = orderRepository;
            _lookupResolver = lookupResolver;
        }

        public Task<PagedResultDTO<PromotionListDTO>> GetPromotionsAsync(PromotionListQueryDTO query, CancellationToken ct)
        {
            return _promotionRepository.GetPromotionsAsync(query, ct);
        }

        public async Task<long> CreatePromotionAsync(
            PromotionDto request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.PromoCode))
                throw new InvalidOperationException("PromoCode is required");

            if (string.IsNullOrWhiteSpace(request.PromoName))
                throw new InvalidOperationException("PromoName is required");

            if (request.StartTime >= request.EndTime)
                throw new InvalidOperationException("EndTime must be greater than StartTime");

            if (request.Type == PromotionTypeCode.PERCENT)
            {
                if (request.DiscountValue <= 0 || request.DiscountValue > 100)
                    throw new InvalidOperationException("Percent must be between 1 and 100");
            }

            if (request.Type == PromotionTypeCode.FIXED_AMOUNT)
            {
                if (request.DiscountValue <= 0)
                    throw new InvalidOperationException("DiscountValue must be greater than 0");
            }

            if (request.MaxUsage < 0)
                throw new InvalidOperationException("MaxUsage must be greater than 0");

            var typeLvId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.PromotionType,
                request.Type,
                ct);

            var status = CalculateStatus(
                request.StartTime,
                request.EndTime);

            var statusLvId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.PromotionStatus,
                status,
                ct);

            var promotion = new Promotion
            {
                PromoCode = request.PromoCode,
                PromoName = request.PromoName,
                Description = request.Description,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DiscountValue = request.DiscountValue,
                MaxUsage = request.MaxUsage == 0 ? null : request.MaxUsage,
                TypeLvId = typeLvId,
                PromotionStatusLvId = statusLvId,
                CreatedAt = DateTime.UtcNow
            };

            // RULES
            foreach (var rule in request.PromotionRules)
            {
                promotion.PromotionRules.Add(new PromotionRule
                {
                    MinOrderValue = rule.MinOrderValue,
                    MinQuantity = rule.MinQuantity,
                    RequiredDishId = rule.RequiredDishId,
                    RequiredCategoryId = rule.RequiredCategoryId
                });
            }

            // TARGETS
            foreach (var target in request.PromotionTargets)
            {
                promotion.PromotionTargets.Add(new PromotionTarget
                {
                    DishId = target.DishId,
                    CategoryId = target.CategoryId
                });
            }

            await _promotionRepository.AddAsync(promotion, ct);
            await _promotionRepository.SaveChangesAsync(ct);

            return promotion.PromotionId;
        }

        public async Task UpdatePromotionAsync(
            PromotionDto request,
            CancellationToken ct)
        {
            if (!request.PromotionId.HasValue)
                throw new Exception("PromotionId is required");

            var promotion = await _promotionRepository
                .GetByIdAsync(request.PromotionId.Value, ct);

            if (promotion == null)
                throw new Exception("Promotion not found");

            var typeLvId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.PromotionType,
                request.Type,
                ct);

            promotion.PromoCode = request.PromoCode;
            promotion.PromoName = request.PromoName;
            promotion.Description = request.Description;
            promotion.StartTime = request.StartTime;
            promotion.EndTime = request.EndTime;
            promotion.DiscountValue = request.DiscountValue;
            promotion.MaxUsage = request.MaxUsage;
            promotion.TypeLvId = typeLvId;

            if (request.PromotionStatus == PromotionStatusCode.DISABLED)
            {
                var disabledId = await _lookupResolver.GetIdAsync(
                    (ushort)Enum.LookupType.PromotionStatus,
                    PromotionStatusCode.DISABLED,
                    ct);

                promotion.PromotionStatusLvId = disabledId;
            }

            // REMOVE OLD RULES
            _promotionRepository.RemoveRules(promotion.PromotionRules);

            promotion.PromotionRules = request.PromotionRules
                .Select(r => new PromotionRule
                {
                    PromotionId = promotion.PromotionId,
                    MinOrderValue = r.MinOrderValue,
                    MinQuantity = r.MinQuantity,
                    RequiredDishId = r.RequiredDishId,
                    RequiredCategoryId = r.RequiredCategoryId
                }).ToList();

            // REMOVE OLD TARGETS
            _promotionRepository.RemoveTargets(promotion.PromotionTargets);

            promotion.PromotionTargets = request.PromotionTargets
                .Select(t => new PromotionTarget
                {
                    PromotionId = promotion.PromotionId,
                    DishId = t.DishId,
                    CategoryId = t.CategoryId
                }).ToList();

            await _promotionRepository.SaveChangesAsync(ct);
        }

        public async Task<PromotionDto> GetPromotionByIdAsync(
            long promotionId,
            CancellationToken ct)
        {
            var promotion = await _promotionRepository
                .GetByIdAsync(promotionId, ct);

            if (promotion == null)
                throw new InvalidOperationException("Promotion not found");

            var dto = new PromotionDto
            {
                PromotionId = promotion.PromotionId,
                PromoCode = promotion.PromoCode,
                PromoName = promotion.PromoName,
                Description = promotion.Description,
                StartTime = promotion.StartTime,
                EndTime = promotion.EndTime,
                DiscountValue = promotion.DiscountValue,
                MaxUsage = promotion.MaxUsage,
                Type = System.Enum.Parse<PromotionTypeCode>(promotion.TypeLv.ValueCode),
                PromotionStatus = System.Enum.Parse<PromotionStatusCode>(promotion.PromotionStatusLv.ValueCode),

                PromotionRules = promotion.PromotionRules
                    .Select(r => new PromotionRuleDto
                    {
                        RuleId = r.RuleId,
                        MinOrderValue = r.MinOrderValue,
                        MinQuantity = r.MinQuantity,
                        RequiredDishId = r.RequiredDishId,
                        RequiredCategoryId = r.RequiredCategoryId
                    }).ToList(),

                PromotionTargets = promotion.PromotionTargets
                    .Select(t => new PromotionTargetDto
                    {
                        TargetId = t.TargetId,
                        DishId = t.DishId,
                        CategoryId = t.CategoryId
                    }).ToList()
            };

            return dto;
        }

        public async Task<PromotionDetailDto> GetPromotionDetailAsync(
        long promotionId,
        CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(promotionId, ct);

            if (promotion == null)
                throw new InvalidOperationException("Promotion not found");

            return new PromotionDetailDto
            {
                PromotionId = promotion.PromotionId,
                PromoCode = promotion.PromoCode!,
                PromoName = promotion.PromoName,
                Description = promotion.Description,
                StartTime = promotion.StartTime,
                EndTime = promotion.EndTime,
                DiscountValue = promotion.DiscountValue,
                MaxUsage = promotion.MaxUsage,
                UsedCount = promotion.UsedCount,

                Type = System.Enum.Parse<PromotionTypeCode>(
                    promotion.TypeLv.ValueCode),

                PromotionStatus = System.Enum.Parse<PromotionStatusCode>(
                    promotion.PromotionStatusLv.ValueCode),

                PromotionRules = promotion.PromotionRules.Select(r => new PromotionRuleDto
                {
                    RuleId = r.RuleId,
                    MinOrderValue = r.MinOrderValue,
                    MinQuantity = r.MinQuantity,
                    RequiredDishId = r.RequiredDishId,
                    RequiredCategoryId = r.RequiredCategoryId
                }).ToList(),

                PromotionTargets = promotion.PromotionTargets.Select(t => new PromotionTargetDto
                {
                    TargetId = t.TargetId,
                    DishId = t.DishId,
                    CategoryId = t.CategoryId
                }).ToList()
            };
        }

        public async Task DisablePromotionAsync(
            long promotionId,
            CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(promotionId, ct);

            if (promotion == null)
                throw new InvalidOperationException("Promotion not found");

            var disableStatusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.PromotionStatus,
                PromotionStatusCode.DISABLED,
                ct);

            promotion.PromotionStatusLvId = disableStatusId;

            await _promotionRepository.SaveChangesAsync(ct);
        }

        public async Task ActivatePromotionAsync(
            long promotionId,
            CancellationToken ct)
        {
            var promotion = await _promotionRepository.GetByIdAsync(promotionId, ct);

            if (promotion == null)
                throw new InvalidOperationException("Promotion not found");

            var now = DateTime.UtcNow;

            PromotionStatusCode status;

            if (now < promotion.StartTime)
                status = PromotionStatusCode.SCHEDULED;
            else if (now > promotion.EndTime)
                status = PromotionStatusCode.EXPIRED;
            else
                status = PromotionStatusCode.ACTIVE;

            var statusId = await _lookupResolver.GetIdAsync(
                (ushort)Enum.LookupType.PromotionStatus,
                status,
                ct);

            promotion.PromotionStatusLvId = statusId;

            await _promotionRepository.SaveChangesAsync(ct);
        }

        public async Task<List<PromotionAvailableDTO>> GetAvailablePromotionsAsync(
        long orderId,
        CancellationToken ct)
        {
            var order = await _orderRepository
                .GetOrderWithItemsAsync(orderId, ct);

            if (order == null)
                throw new Exception("Order not found");

            var promotions = await _promotionRepository
                .GetActivePromotionsAsync(DateTime.UtcNow, ct);

            var result = new List<PromotionAvailableDTO>();

            foreach (var promo in promotions)
            {
                var ruleCheck = CheckRules(order, promo);

                if (!ruleCheck.IsValid)
                {
                    continue;
                }

                var discount = CalculateDiscount(order, promo);
                var appliedRule = FormatRule(promo.PromotionRules.FirstOrDefault());

                if (appliedRule == null && !promo.PromotionTargets.Any())
                {
                    appliedRule = new Dictionary<string, string>
                    {
                        { "vi", "Sự kiện khuyến mãi" },
                        { "en", "Promotion event" },
                        { "fr", "Événement promotionnel" }
                    };
                }

                result.Add(new PromotionAvailableDTO
                {
                    PromotionId = promo.PromotionId,
                    PromoCode = promo.PromoCode,
                    PromoName = promo.PromoName,
                    PromotionType = promo.TypeLv.ValueCode,
                    HasTarget = promo.PromotionTargets.Any(),
                    DiscountValue = promo.DiscountValue,
                    AppliedRule = appliedRule,
                    EstimatedDiscount = discount,
                    FinalAmount = order.TotalAmount - discount,
                    TargetDishIds = promo.PromotionTargets.Where(t => t.DishId.HasValue).Select(t => t.DishId!.Value).ToList(),
                    TargetCategoryIds = promo.PromotionTargets.Where(t => t.CategoryId.HasValue).Select(t => t.CategoryId!.Value).ToList()
                });
            }

            return result;
        }

        public async Task DeletePromotionAsync(
            long promotionId,
            CancellationToken ct)
        {
            var promotion = await _promotionRepository
                .GetByIdWithRelationsAsync(promotionId, ct);

            if (promotion == null)
                throw new KeyNotFoundException("Promotion not found");

            if (promotion.OrderPromotions.Any())
                throw new InvalidOperationException(
                    "Cannot delete promotion that has already been used in orders");

            if (promotion.PromotionRules.Any())
                _promotionRepository.RemoveRules(promotion.PromotionRules);

            if (promotion.PromotionTargets.Any())
                _promotionRepository.RemoveTargets(promotion.PromotionTargets);

            await _promotionRepository.DeleteAsync(promotion, ct);

            await _promotionRepository.SaveChangesAsync(ct);
        }

        private PromotionStatusCode CalculateStatus(
            DateTime start,
            DateTime end)
        {
            var now = DateTime.UtcNow;

            if (now < start)
                return PromotionStatusCode.SCHEDULED;

            if (now >= start && now <= end)
                return PromotionStatusCode.ACTIVE;

            return PromotionStatusCode.EXPIRED;
        }

        private (bool IsValid, string? Reason) CheckRules(Order order, Promotion promo)
        {
            var rule = promo.PromotionRules.FirstOrDefault();

            if (rule == null)
                return (true, null);

            if (rule.MinOrderValue.HasValue &&
                order.TotalAmount < rule.MinOrderValue.Value)
            {
                return (false, "Order value not enough");
            }

            if (rule.MinQuantity.HasValue)
            {
                var totalQty = order.OrderItems.Sum(x => x.Quantity);

                if (totalQty < rule.MinQuantity.Value)
                    return (false, "Quantity not enough");
            }

            if (rule.RequiredDishId.HasValue)
            {
                var hasDish = order.OrderItems
                    .Any(x => x.DishId == rule.RequiredDishId.Value);

                if (!hasDish)
                    return (false, "Required dish missing");
            }

            if (rule.RequiredCategoryId.HasValue)
            {
                var hasCategory = order.OrderItems
                    .Any(x => x.Dish.CategoryId == rule.RequiredCategoryId.Value);

                if (!hasCategory)
                    return (false, "Required category missing");
            }

            return (true, null);
        }

        private decimal CalculateDiscount(Order order, Promotion promo)
        {
            decimal targetAmount = GetTargetAmount(order, promo);

            if (promo.TypeLv.ValueCode == "PERCENT")
            {
                return targetAmount * promo.DiscountValue / 100;
            }

            if (promo.TypeLv.ValueCode == "FIXED_AMOUNT")
            {
                return Math.Min(promo.DiscountValue, targetAmount);
            }

            return 0;
        }

        private decimal GetTargetAmount(Order order, Promotion promo)
        {
            if (!promo.PromotionTargets.Any())
                return order.TotalAmount;

            decimal total = 0;

            foreach (var item in order.OrderItems)
            {
                foreach (var target in promo.PromotionTargets)
                {
                    if (target.DishId == item.DishId)
                        total += item.Price * item.Quantity;

                    if (target.CategoryId == item.Dish.CategoryId)
                        total += item.Price * item.Quantity;
                }
            }

            return total;
        }

        private Dictionary<string, string>? FormatRule(PromotionRule? rule)
        {
            if (rule == null) return null;

            var results = new Dictionary<string, string>();
            var languages = new[] { "vi", "en", "fr" };

            foreach (var lang in languages)
            {
                var parts = new List<string>();

                if (rule.MinOrderValue.HasValue)
                {
                    parts.Add(lang switch
                    {
                        "vi" => $"Hóa đơn tối thiểu: {rule.MinOrderValue.Value:N0} CHF",
                        "fr" => $"Commande minimum: {rule.MinOrderValue.Value:N0} CHF",
                        _ => $"Min order value: {rule.MinOrderValue.Value:N0} CHF"
                    });
                }

                if (rule.MinQuantity.HasValue)
                {
                    parts.Add(lang switch
                    {
                        "vi" => $"Số lượng tối thiểu: {rule.MinQuantity.Value} món",
                        "fr" => $"Quantité minimum: {rule.MinQuantity.Value} plats",
                        _ => $"Min quantity: {rule.MinQuantity.Value} items"
                    });
                }

                if (rule.RequiredDishId.HasValue && rule.RequiredDish != null)
                {
                    var dishName = GetTranslation(rule.RequiredDish.DishNameText, lang) ?? rule.RequiredDish.DishName;
                    parts.Add(lang switch
                    {
                        "vi" => $"Yêu cầu món: {dishName}",
                        "fr" => $"Plat requis: {dishName}",
                        _ => $"Required dish: {dishName}"
                    });
                }

                if (rule.RequiredCategoryId.HasValue && rule.RequiredCategory != null)
                {
                    var categoryName = GetTranslation(rule.RequiredCategory.CategoryNameText, lang) ?? rule.RequiredCategory.CategoryName;
                    parts.Add(lang switch
                    {
                        "vi" => $"Yêu cầu danh mục: {categoryName}",
                        "fr" => $"Catégorie requise: {categoryName}",
                        _ => $"Required category: {categoryName}"
                    });
                }

                results[lang] = string.Join(", ", parts);
            }

            return results;
        }

        private string? GetTranslation(I18nText? text, string langCode)
        {
            if (text == null) return null;
            return text.I18nTranslations.FirstOrDefault(t => t.LangCode == langCode)?.TranslatedText ?? text.SourceText;
        }
    }
}

using Core.Data;
using Core.DTO.Notification;
using Core.DTO.Order;
using Core.DTO.Shift;
using Core.Entity;
using Core.Enum;
using LookupTypeEnum = Core.Enum.LookupType;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Core.Interface.Service.Shift;
using Infa.Data;
using Microsoft.EntityFrameworkCore;
using Core.DTO.General;
using Core.DTO.Payment;
using Infa.Repo;

namespace Infa.Service;

public class PaymentService : IPaymentService
{
    private const string LoyaltyEnabledSettingKey = "loyalty.enabled";
    private const string LoyaltyPointBaseSettingKey = "loyalty.point_base_amount";
    private const long GuestCustomerId = 68;

    private readonly RestaurantMgmtContext _context;
    private readonly ILookupResolver _lookupResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemSettingService _systemSettingService;
    private readonly INotificationService _notificationService;
    private readonly IShiftLiveRealtimePublisher _shiftLiveRealtimePublisher;
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(
        RestaurantMgmtContext context,
        ILookupResolver lookupResolver,
        IUnitOfWork unitOfWork,
        ISystemSettingService systemSettingService,
        INotificationService notificationService,
        IShiftLiveRealtimePublisher shiftLiveRealtimePublisher,
        IPaymentRepository paymentRepository)
    {
        _context = context;
        _lookupResolver = lookupResolver;
        _unitOfWork = unitOfWork;
        _systemSettingService = systemSettingService;
        _notificationService = notificationService;
        _shiftLiveRealtimePublisher = shiftLiveRealtimePublisher;
        _paymentRepository = paymentRepository;
    }

    public async Task ProcessPaymentAsync(CreatePaymentDTO dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.OrderCoupons)
            .Include(o => o.OrderPromotions)
            .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {dto.OrderId} not found");

        var cancelledStatusId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var completedStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
        var rejectedItemStatusId = await OrderItemStatusCode.REJECTED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var cancelledItemStatusId = await OrderItemStatusCode.CANCELLED.ToOrderItemStatusIdAsync(_lookupResolver, cancellationToken);
        var activeCouponStatusId = await CouponStatusCode.ACTIVE.IdAsync(_lookupResolver, (ushort)LookupTypeEnum.CouponStatus, cancellationToken);
        var activePromotionStatusId = await PromotionStatusCode.ACTIVE.ToPromotionStatusIdAsync(_lookupResolver, cancellationToken);

        if (order.OrderStatusLvId == cancelledStatusId)
        {
            throw new InvalidOperationException("Cannot pay for a cancelled order.");
        }

        if (order.Payments.Any())
        {
            throw new InvalidOperationException("This order has already been paid.");
        }

        // Resolve payment method ID
        var methodLvId = await dto.PaymentMethod.IdAsync(_lookupResolver, (ushort)LookupTypeEnum.PaymentMethod, cancellationToken);
        var now = DateTime.UtcNow;
        var (loyaltyEnabled, loyaltyPointBaseAmount) = await ResolveLoyaltyPolicyAsync(cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var subTotal = order.OrderItems
                .Where(oi => oi.ItemStatusLvId != rejectedItemStatusId && oi.ItemStatusLvId != cancelledItemStatusId)
                .Sum(oi => oi.Price * oi.Quantity);

            var tipAmount = dto.TipAmount ?? order.TipAmount ?? 0m;
            decimal totalDiscountAmount = 0m;

            if (dto.CouponIds != null && dto.CouponIds.Any())
            {
                foreach (var couponId in dto.CouponIds.Distinct())
                {
                    var coupon = await _context.Coupons
                        .Include(c => c.TypeLv)
                        .FirstOrDefaultAsync(c => c.CouponId == couponId, cancellationToken)
                        ?? throw new InvalidOperationException($"Coupon {couponId} not found.");

                    if (coupon.CustomerId.HasValue && coupon.CustomerId.Value != order.CustomerId)
                    {
                        throw new InvalidOperationException($"Coupon {coupon.CouponCode} does not belong to this customer.");
                    }

                    if (coupon.CouponStatusLvId != activeCouponStatusId)
                        throw new InvalidOperationException($"Coupon {coupon.CouponCode} is not active.");

                    if (now < coupon.StartTime || now > coupon.EndTime)
                        throw new InvalidOperationException($"Coupon {coupon.CouponCode} is outside its valid period.");

                    if (coupon.MaxUsage.HasValue && (coupon.UsedCount ?? 0) >= coupon.MaxUsage.Value)
                        throw new InvalidOperationException($"Coupon {coupon.CouponCode} usage limit has been reached.");

                    var existingOrderCoupon = order.OrderCoupons.FirstOrDefault(oc => oc.CouponId == coupon.CouponId);
                    if (existingOrderCoupon is null)
                    {
                        var couponDiscountAmount = CalculateDiscountAmount(coupon.TypeLv.ValueCode, coupon.DiscountValue, subTotal);
                        order.OrderCoupons.Add(new OrderCoupon
                        {
                            CouponId = coupon.CouponId,
                            DiscountAmount = couponDiscountAmount,
                            AppliedAt = now
                        });
                        coupon.UsedCount = (coupon.UsedCount ?? 0) + 1;
                        totalDiscountAmount += couponDiscountAmount;
                    }
                    else
                    {
                        totalDiscountAmount += existingOrderCoupon.DiscountAmount;
                    }
                }
            }

            // Handle Promotions (Automatic)
            var allActivePromotions = await _context.Promotions
                .Include(p => p.TypeLv)
                .Include(p => p.PromotionRules)
                .Include(p => p.PromotionTargets)
                .Where(p => p.PromotionStatusLvId == activePromotionStatusId)
                .Where(p => now >= p.StartTime && now <= p.EndTime)
                .Where(p => !p.MaxUsage.HasValue || (p.UsedCount ?? 0) < p.MaxUsage.Value)
                .ToListAsync(cancellationToken);

            var promotionDiscounts = new Dictionary<long, decimal>();

            // 1. Automatic Dish-Level Promotions
            var autoPromos = allActivePromotions.Where(p => p.PromotionTargets.Any()).ToList();
            var orderItems = order.OrderItems
                .Where(oi => oi.ItemStatusLvId != rejectedItemStatusId && oi.ItemStatusLvId != cancelledItemStatusId)
                .ToList();

            foreach (var item in orderItems)
            {
                foreach (var promo in autoPromos)
                {
                    // Check if item matches any target
                    if (promo.PromotionTargets.Any(t => (t.DishId.HasValue && t.DishId == item.DishId) || 
                                                        (t.CategoryId.HasValue && t.CategoryId == item.Dish.CategoryId)))
                    {
                        // Check rules
                        var rule = promo.PromotionRules.FirstOrDefault();
                        if (rule != null)
                        {
                            if (rule.MinOrderValue.HasValue && subTotal < rule.MinOrderValue.Value) continue;
                            if (rule.RequiredDishId.HasValue && !orderItems.Any(oi => oi.DishId == rule.RequiredDishId.Value)) continue;
                            if (rule.RequiredCategoryId.HasValue && !orderItems.Any(oi => oi.Dish.CategoryId == rule.RequiredCategoryId.Value)) continue;
                        }

                        var discount = CalculateDiscountAmount(promo.TypeLv.ValueCode, promo.DiscountValue, item.Price * item.Quantity);
                        
                        if (promotionDiscounts.ContainsKey(promo.PromotionId))
                            promotionDiscounts[promo.PromotionId] += discount;
                        else
                            promotionDiscounts[promo.PromotionId] = discount;
                    }
                }
            }

            // 2. Automatic General Promotions (no targets)
            var generalPromos = allActivePromotions.Where(p => !p.PromotionTargets.Any()).ToList();
            foreach (var promo in generalPromos)
            {
                // Check rules
                var rule = promo.PromotionRules.FirstOrDefault();
                if (rule != null)
                {
                    if (rule.MinOrderValue.HasValue && subTotal < rule.MinOrderValue.Value) continue;
                    if (rule.RequiredDishId.HasValue && !orderItems.Any(oi => oi.DishId == rule.RequiredDishId.Value)) continue;
                    if (rule.RequiredCategoryId.HasValue && !orderItems.Any(oi => oi.Dish.CategoryId == rule.RequiredCategoryId.Value)) continue;
                }

                var discount = CalculateDiscountAmount(promo.TypeLv.ValueCode, promo.DiscountValue, subTotal);
                if (promotionDiscounts.ContainsKey(promo.PromotionId))
                    promotionDiscounts[promo.PromotionId] += discount;
                else
                    promotionDiscounts[promo.PromotionId] = discount;
            }

            // 3. Apply all calculated promotions
            foreach (var pd in promotionDiscounts)
            {
                var promotion = allActivePromotions.First(p => p.PromotionId == pd.Key);
                
                // Add OrderPromotion record
                order.OrderPromotions.Add(new OrderPromotion
                {
                    PromotionId = promotion.PromotionId,
                    DiscountAmount = pd.Value,
                    AppliedAt = now
                });

                promotion.UsedCount = (promotion.UsedCount ?? 0) + 1;
                totalDiscountAmount += pd.Value;
            }

            var finalAmount = Math.Max(0m, subTotal + order.TaxAmount + tipAmount - totalDiscountAmount);
            if (dto.ReceivedAmount < finalAmount)
            {
                throw new InvalidOperationException("Received amount cannot be less than final amount.");
            }

            // Create Payment record
            var payment = new Payment
            {
                OrderId = dto.OrderId,
                ReceivedAmount = dto.ReceivedAmount,
                ChangeAmount = Math.Max(0m, dto.ReceivedAmount - finalAmount),
                PaidAt = DateTime.UtcNow,
                MethodLvId = methodLvId
            };

            _context.Payments.Add(payment);

            // Update Order status to COMPLETED
            order.SubTotalAmount = subTotal;
            order.TotalAmount = finalAmount;
            order.OrderStatusLvId = completedStatusId;
            order.UpdatedAt = DateTime.UtcNow;
            order.TipAmount = tipAmount;

            // Update Table status to AVAILABLE if it's a dine-in order
            if (order.TableId.HasValue)
            {
                var table = await _context.RestaurantTables.FirstOrDefaultAsync(t => t.TableId == order.TableId.Value, cancellationToken);
                if (table != null)
                {
                    table.TableStatusLvId = availableTableStatusId;
                    table.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (loyaltyEnabled && order.CustomerId != GuestCustomerId)
            {
                var earnedPoints = CalculateLoyaltyPoints(finalAmount, loyaltyPointBaseAmount);
                if (earnedPoints > 0)
                {
                    order.Customer.LoyaltyPoints = (order.Customer.LoyaltyPoints ?? 0) + earnedPoints;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Notify about payment completion
            await _notificationService.PublishAsync(new PublishNotificationRequest
            {
                Type = nameof(NotificationType.PAYMENT_COMPLETED),
                Title = "Payment Completed",
                Body = $"Payment for Order #{dto.OrderId} completed",
                Priority = nameof(NotificationPriority.Normal),
                SoundKey = "notification_normal",
                ActionUrl = $"/dashboard/invoices",
                EntityType = "Order",
                EntityId = dto.OrderId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["orderId"] = dto.OrderId.ToString(),
                    ["amount"] = finalAmount.ToString("F2"),
                    ["method"] = dto.PaymentMethod.ToString()
                },
                TargetPermissions = new List<string> { Permissions.ViewOrder }
            }, cancellationToken);

            await _shiftLiveRealtimePublisher.PublishBoardChangedAsync(new ShiftLiveRealtimeEventDto
            {
                EventType = "payment_completed",
                WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
                OrderId = dto.OrderId,
                StaffId = order.StaffId,
                OccurredAt = DateTime.UtcNow,
            }, cancellationToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task<PagedResultDTO<PaymentListDTO>> GetPaymentsAsync(
        PaymentListQueryDTO query,
        CancellationToken ct)
    {
        return _paymentRepository.GetPaymentsAsync(query, ct);
    }

    private static decimal CalculateDiscountAmount(string discountType, decimal discountValue, decimal baseAmount)
    {
        if (baseAmount <= 0)
        {
            return 0m;
        }

        if (string.Equals(discountType, CouponTypeCode.PERCENT.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(discountType, PromotionTypeCode.PERCENT.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var percentDiscount = Math.Round(baseAmount * (discountValue / 100m), 2);
            return Math.Min(percentDiscount, baseAmount);
        }

        return Math.Min(discountValue, baseAmount);
    }

    private async Task<(bool Enabled, decimal PointBaseAmount)> ResolveLoyaltyPolicyAsync(CancellationToken cancellationToken)
    {
        var loyaltyEnabled = (await _systemSettingService.GetBoolAsync(
            LoyaltyEnabledSettingKey,
            defaultValue: false,
            cancellationToken: cancellationToken)) ?? false;

        if (!loyaltyEnabled)
        {
            return (false, 0m);
        }

        var pointBaseAmount = (await _systemSettingService.GetDecimalAsync(
            LoyaltyPointBaseSettingKey,
            defaultValue: null,
            cancellationToken: cancellationToken)) ?? 0m;

        if (pointBaseAmount <= 0)
        {
            throw new InvalidOperationException("Loyalty is enabled but loyalty.point_base_amount is missing or invalid.");
        }

        return (true, pointBaseAmount);
    }

    private static int CalculateLoyaltyPoints(decimal finalAmount, decimal pointBaseAmount)
    {
        if (finalAmount <= 0 || pointBaseAmount <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(finalAmount / pointBaseAmount);
    }
}

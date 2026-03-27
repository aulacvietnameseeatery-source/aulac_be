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

            if (dto.CouponId.HasValue)
            {
                var coupon = await _context.Coupons
                    .Include(c => c.TypeLv)
                    .FirstOrDefaultAsync(c => c.CouponId == dto.CouponId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Coupon not found.");

                if (coupon.CouponStatusLvId != activeCouponStatusId)
                {
                    throw new InvalidOperationException("Coupon is not active.");
                }

                if (now < coupon.StartTime || now > coupon.EndTime)
                {
                    throw new InvalidOperationException("Coupon is outside its valid period.");
                }

                if (coupon.MaxUsage.HasValue && (coupon.UsedCount ?? 0) >= coupon.MaxUsage.Value)
                {
                    throw new InvalidOperationException("Coupon usage limit has been reached.");
                }

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

            if (dto.PromotionId.HasValue)
            {
                var promotion = await _context.Promotions
                    .Include(p => p.TypeLv)
                    .FirstOrDefaultAsync(p => p.PromotionId == dto.PromotionId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Promotion not found.");

                if (promotion.PromotionStatusLvId != activePromotionStatusId)
                {
                    throw new InvalidOperationException("Promotion is not active.");
                }

                if (now < promotion.StartTime || now > promotion.EndTime)
                {
                    throw new InvalidOperationException("Promotion is outside its valid period.");
                }

                if (promotion.MaxUsage.HasValue && (promotion.UsedCount ?? 0) >= promotion.MaxUsage.Value)
                {
                    throw new InvalidOperationException("Promotion usage limit has been reached.");
                }

                var existingOrderPromotion = order.OrderPromotions.FirstOrDefault(op => op.PromotionId == promotion.PromotionId);
                if (existingOrderPromotion is null)
                {
                    var promotionDiscountAmount = CalculateDiscountAmount(promotion.TypeLv.ValueCode, promotion.DiscountValue, subTotal);

                    order.OrderPromotions.Add(new OrderPromotion
                    {
                        PromotionId = promotion.PromotionId,
                        DiscountAmount = promotionDiscountAmount,
                        AppliedAt = now
                    });

                    promotion.UsedCount = (promotion.UsedCount ?? 0) + 1;
                    totalDiscountAmount += promotionDiscountAmount;
                }
                else
                {
                    totalDiscountAmount += existingOrderPromotion.DiscountAmount;
                }
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

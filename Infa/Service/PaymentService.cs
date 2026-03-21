using Core.Data;
using Core.DTO.Notification;
using Core.DTO.Order;
using Core.Entity;
using Core.Enum;
using LookupTypeEnum = Core.Enum.LookupType;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Notification;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Service;

public class PaymentService : IPaymentService
{
    private readonly RestaurantMgmtContext _context;
    private readonly ILookupResolver _lookupResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public PaymentService(
        RestaurantMgmtContext context,
        ILookupResolver lookupResolver,
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _context = context;
        _lookupResolver = lookupResolver;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task ProcessPaymentAsync(CreatePaymentDTO dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {dto.OrderId} not found");

        var cancelledStatusId = await OrderStatusCode.CANCELLED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var completedStatusId = await OrderStatusCode.COMPLETED.ToOrderStatusIdAsync(_lookupResolver, cancellationToken);
        var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

        if (order.OrderStatusLvId == cancelledStatusId)
        {
            throw new InvalidOperationException("Cannot pay for a cancelled order.");
        }

        // Resolve payment method ID
        var methodLvId = await dto.PaymentMethod.IdAsync(_lookupResolver, (ushort)LookupTypeEnum.PaymentMethod, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create Payment record
            var payment = new Payment
            {
                OrderId = dto.OrderId,
                ReceivedAmount = dto.ReceivedAmount,
                ChangeAmount = Math.Max(0, dto.ReceivedAmount - order.TotalAmount),
                PaidAt = DateTime.UtcNow,
                MethodLvId = methodLvId
            };

            _context.Payments.Add(payment);

            // Update Order status to COMPLETED
            order.OrderStatusLvId = completedStatusId;
            order.UpdatedAt = DateTime.UtcNow;

            if (dto.TipAmount.HasValue)
            {
                order.TipAmount = dto.TipAmount;
            }

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
                    ["amount"] = dto.ReceivedAmount.ToString("F2"),
                    ["method"] = dto.PaymentMethod.ToString()
                },
                TargetPermissions = new List<string> { Permissions.ViewOrder }
            }, cancellationToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

using Core.DTO.Order;
using Core.Entity;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Infa.Data;
using Microsoft.EntityFrameworkCore;

namespace Infa.Service;

public class PaymentService : IPaymentService
{
    private readonly RestaurantMgmtContext _context;
    private readonly ILookupResolver _lookupResolver;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        RestaurantMgmtContext context,
        ILookupResolver lookupResolver,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _lookupResolver = lookupResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task ProcessPaymentAsync(CreatePaymentDTO dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {dto.OrderId} not found");

        if (order.OrderStatusLvId == 31) // CANCELLED
        {
            throw new InvalidOperationException("Cannot pay for a cancelled order.");
        }

        // Resolve payment method ID
        var methodLvId = await _lookupResolver.GetIdAsync((ushort)Core.Enum.LookupType.PaymentMethod, dto.PaymentMethod, cancellationToken);

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

            // Update Order status to COMPLETED (if it wasn't already)
            // Even if it's already completed, we ensure it is.
            order.OrderStatusLvId = 30; // COMPLETED
            order.UpdatedAt = DateTime.UtcNow;

            if (dto.TipAmount.HasValue)
            {
                order.TipAmount = dto.TipAmount;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

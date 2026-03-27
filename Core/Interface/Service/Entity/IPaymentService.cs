using Core.DTO.General;
using Core.DTO.Order;
using Core.DTO.Payment;

namespace Core.Interface.Service.Entity;

public interface IPaymentService
{
    Task ProcessPaymentAsync(CreatePaymentDTO dto, CancellationToken cancellationToken = default);

    Task<PagedResultDTO<PaymentListDTO>> GetPaymentsAsync(PaymentListQueryDTO query, CancellationToken ct);
}

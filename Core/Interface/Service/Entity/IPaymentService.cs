using Core.DTO.Order;

namespace Core.Interface.Service.Entity;

public interface IPaymentService
{
    Task ProcessPaymentAsync(CreatePaymentDTO dto, CancellationToken cancellationToken = default);
}

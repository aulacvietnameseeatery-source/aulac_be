using Core.DTO.General;
using Core.DTO.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Repo
{
    public interface IPaymentRepository
    {
        Task<PagedResultDTO<PaymentListDTO>> GetPaymentsAsync(PaymentListQueryDTO query, CancellationToken ct);
    }
}

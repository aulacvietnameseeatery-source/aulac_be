using Core.DTO.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Email
{
    public interface IEmailQueue
    {
        Task EnqueueAsync(QueuedEmail email, CancellationToken ct = default);
        Task<QueuedEmail> DequeueAsync(CancellationToken ct = default);
    }
    
}

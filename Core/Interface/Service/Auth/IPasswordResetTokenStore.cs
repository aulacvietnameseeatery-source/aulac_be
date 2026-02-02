using Core.DTO.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Auth
{
    public interface IPasswordResetTokenStore
    {
        Task StoreAsync(PasswordResetTokenRecord record, TimeSpan ttl, CancellationToken ct);
        Task<PasswordResetTokenRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
        Task ConsumeAsync(string tokenHash, CancellationToken ct);
        Task InvalidateUserAsync(long userId, CancellationToken ct);
    }

}

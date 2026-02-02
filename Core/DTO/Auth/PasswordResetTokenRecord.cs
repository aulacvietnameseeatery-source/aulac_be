using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Auth
{
    /// <summary>
    /// Record representing a password reset token stored in cached service.
    /// </summary>
    public sealed record PasswordResetTokenRecord(
        long UserId,
        string EmailNormalized,
        string TokenHash,
        DateTimeOffset ExpiresAt,
        DateTimeOffset IssuedAt
    );

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Email
{
    public sealed record DeadLetterEmail(
        QueuedEmail Email,
        string Error,
        int Attempt,
        DateTimeOffset FailedAt
    );
}

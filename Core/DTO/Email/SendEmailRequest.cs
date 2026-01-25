
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Email
{
    public sealed record QueuedEmail(
    string To,
    string Subject,
    string HtmlBody,
    string? CorrelationId = null
);
}

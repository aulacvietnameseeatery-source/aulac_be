using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO.Email
{
    public sealed record EmailMessage(
        string To,
        string Subject,
        string HtmlBody,
        string? From = null,
        string? FromName = null
    );
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infa.Email;

public sealed class SmtpOptions
{
    public string Host { get; init; } = default!;
    public int Port { get; init; } = 587;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool UseStartTls { get; init; } = true;

    public string DefaultFrom { get; init; } = default!;
    public string DefaultFromName { get; init; } = "No-Reply";
}


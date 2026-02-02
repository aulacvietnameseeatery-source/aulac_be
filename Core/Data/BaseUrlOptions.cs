using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public sealed class BaseUrlOptions
    {
        public string Client { get; init; } = default!;
        public string Api { get; init; } = default!;
    }
}

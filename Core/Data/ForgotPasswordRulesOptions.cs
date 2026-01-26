using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public sealed class ForgotPasswordRulesOptions
    {
        public int MaxAttempts { get; init; } = 5;
        public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);

        // Recommended additions
        public int TokenLengthBytes { get; init; } = 32;          // 256-bit
        public int TokenLifetimeMinutes { get; init; } = 30;      // 30 minutes
    }

}

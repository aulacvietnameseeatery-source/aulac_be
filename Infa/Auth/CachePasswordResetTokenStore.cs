using Core.DTO.Auth;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Others;

namespace Infa.Auth
{
    /// <summary>
    /// Cache-based password reset token store implementation.
    /// Works with any ICacheService implementation (Redis, In-Memory, etc.)
    /// </summary>
    public sealed class CachePasswordResetTokenStore : IPasswordResetTokenStore
    {
        private readonly ICacheService _cache;

        private const string TokenPrefix = "pwdreset:";      // pwdreset:{hash}
        private const string UserPrefix = "pwdreset:u:";    // pwdreset:u:{userId}

        public CachePasswordResetTokenStore(ICacheService cache) => _cache = cache;

        public async Task StoreAsync(PasswordResetTokenRecord record, TimeSpan ttl, CancellationToken ct)
        {
            var tokenKey = TokenPrefix + record.TokenHash;
            var userKey = UserPrefix + record.UserId.ToString("N");

            await _cache.SetAsync(tokenKey, record, ttl, ct);
            await _cache.SetAsync(userKey, record.TokenHash, ttl, ct);
        }

        public Task<PasswordResetTokenRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
    => _cache.GetAsync<PasswordResetTokenRecord>(TokenPrefix + tokenHash, ct);

        public Task ConsumeAsync(string tokenHash, CancellationToken ct)
                    => _cache.RemoveAsync(TokenPrefix + tokenHash, ct);

        public async Task InvalidateUserAsync(long userId, CancellationToken ct)
        {
            var userKey = UserPrefix + userId.ToString("N");
            var existingHash = await _cache.GetAsync<string>(userKey, ct);

            if (!string.IsNullOrWhiteSpace(existingHash))
            {
                await _cache.RemoveAsync(TokenPrefix + existingHash!, ct);
                await _cache.RemoveAsync(userKey, ct);
            }
        }
    }
}

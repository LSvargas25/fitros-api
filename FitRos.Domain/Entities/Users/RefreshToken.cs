using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Entities.Users
{
    public sealed class RefreshToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public string? ReplacedByTokenHash { get; private set; }

        private RefreshToken() { } // EF

        private RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
        }

        public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
            => new(userId, tokenHash, expiresAt);

        public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;

        public void Revoke(DateTime utcNow, string? replacedByTokenHash = null)
        {
            if (IsRevoked) return;
            RevokedAt = utcNow;
            ReplacedByTokenHash = replacedByTokenHash;
        }
    }
}
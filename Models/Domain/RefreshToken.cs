using System.ComponentModel.DataAnnotations;

namespace MessagingApp.Models.Domain
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        public string Token { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; } = false;

        public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}
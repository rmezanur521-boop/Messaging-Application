namespace MessagingApp.Models.Domain
{
    public class Friendship
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FriendId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AppUser User { get; set; } = null!;
        public AppUser Friend { get; set; } = null!;
    }
}
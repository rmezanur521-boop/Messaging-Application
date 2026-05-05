namespace MessagingApp.Models.Domain
{
    public enum FriendRequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class FriendRequest
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public AppUser Sender { get; set; } = null!;
        public AppUser Receiver { get; set; } = null!;
    }
}
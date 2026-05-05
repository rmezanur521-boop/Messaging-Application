namespace MessagingApp.Models.Domain
{
    public class GroupMessage
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public Group Group { get; set; } = null!;
        public AppUser Sender { get; set; } = null!;
    }
}
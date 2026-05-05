namespace MessagingApp.Models.ViewModels
{
    public class ConversationViewModel
    {
        public string FriendId { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public string? FriendProfilePicture { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public List<MessageViewModel> Messages { get; set; } = new();
    }

    public class MessageViewModel
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsMine { get; set; }
    }
}
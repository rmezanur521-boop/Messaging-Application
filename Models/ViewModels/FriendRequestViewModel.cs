namespace MessagingApp.Models.ViewModels
{
    public class FriendRequestViewModel
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? SenderProfilePicture { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class FriendViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsFriend { get; set; }
        public bool IsRequestSent { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
    }

    public class FriendIndexViewModel
    {
        public List<FriendViewModel> Friends { get; set; } = new();
        public List<FriendRequestViewModel> PendingRequests { get; set; } = new();
        public List<FriendViewModel> SearchResults { get; set; } = new();
        public string? SearchQuery { get; set; }
    }
}
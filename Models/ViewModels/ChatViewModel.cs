namespace MessagingApp.Models.ViewModels
{
    public class ChatViewModel
    {
        public List<ChatPreviewViewModel> Conversations { get; set; } = new();
        public List<GroupPreviewViewModel> Groups { get; set; } = new();
    }

    public class ChatPreviewViewModel
    {
        public string FriendId { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public string? FriendProfilePicture { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
    }

    public class GroupPreviewViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int MemberCount { get; set; }
    }
}
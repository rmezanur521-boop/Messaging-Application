namespace MessagingApp.Models.ViewModels
{
    public class CreateGroupViewModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string> SelectedFriendIds { get; set; } = new();
        public List<FriendSelectItem> Friends { get; set; } = new();
    }

    public class FriendSelectItem
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
    }

    public class GroupChatViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public List<MessageViewModel> Messages { get; set; } = new();
        public List<GroupMemberViewModel> Members { get; set; } = new();
    }

    public class GroupMemberViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class ManageGroupViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<GroupMemberViewModel> Members { get; set; } = new();
        public List<FriendSelectItem> FriendsNotInGroup { get; set; } = new();
    }
}
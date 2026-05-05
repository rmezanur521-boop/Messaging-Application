namespace MessagingApp.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFriend { get; set; }
        public bool HasPendingRequest { get; set; }
        public bool RequestSentByMe { get; set; }
        public int FriendRequestId { get; set; }
    }
}
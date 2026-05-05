namespace MessagingApp.Models.Domain
{
    public enum GroupRole
    {
        Member,
        Admin
    }

    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public GroupRole Role { get; set; } = GroupRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public Group Group { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
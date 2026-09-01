using MessagingApp.Models.ViewModels;

namespace MessagingApp.Services.Interfaces
{
    public interface IGroupService
    {
        Task<int> CreateGroupAsync(string creatorId, string name, List<string> memberIds);
        Task<GroupChatViewModel?> GetGroupChatAsync(int groupId, string currentUserId);
        Task<ManageGroupViewModel?> GetManageGroupAsync(int groupId, string currentUserId);
        Task<bool> AddMemberAsync(int groupId, string userId, string adminId);
        Task<bool> RemoveMemberAsync(int groupId, string userId, string adminId);
        Task<MessageViewModel> SaveGroupMessageAsync(int groupId, string senderId, string content);
        Task<MessageViewModel?> EditGroupMessageAsync(int messageId, string userId, string newContent);
        Task<bool> DeleteGroupMessageAsync(int messageId, string userId);
        Task<List<GroupPreviewViewModel>> GetGroupPreviewsAsync(string userId);
        Task<bool> IsGroupAdminAsync(int groupId, string userId);
        Task<bool> IsMemberAsync(int groupId, string userId);
        Task<GroupLeaveResult> LeaveGroupAsync(int groupId, string userId);
        Task<bool> DeleteGroupAsync(int groupId, string userId);
    }
}
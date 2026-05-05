using MessagingApp.Models.ViewModels;

namespace MessagingApp.Services.Interfaces
{
    public interface IChatService
    {
        Task<List<MessageViewModel>> GetConversationAsync(string userId1, string userId2, string currentUserId);
        Task<MessageViewModel> SaveMessageAsync(string senderId, string receiverId, string content);
        Task<MessageViewModel?> EditMessageAsync(int messageId, string userId, string newContent);
        Task<bool> DeleteMessageAsync(int messageId, string userId);
        Task<List<ChatPreviewViewModel>> GetChatPreviewsAsync(string userId);
    }
}
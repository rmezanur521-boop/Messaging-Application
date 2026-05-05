using MessagingApp.Models.ViewModels;

namespace MessagingApp.Services.Interfaces
{
    public interface IFriendService
    {
        Task<List<FriendViewModel>> GetFriendsAsync(string userId);
        Task<List<FriendRequestViewModel>> GetPendingRequestsAsync(string userId);
        Task<List<FriendViewModel>> SearchUsersAsync(string query, string currentUserId);
        Task<bool> SendFriendRequestAsync(string senderId, string receiverId);
        Task<bool> AcceptFriendRequestAsync(int requestId, string currentUserId);
        Task<bool> RejectFriendRequestAsync(int requestId, string currentUserId);
        Task<bool> AreFriendsAsync(string userId1, string userId2);
        Task<bool> RemoveFriendAsync(string userId, string friendId);
    }
}
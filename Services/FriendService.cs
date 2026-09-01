using MessagingApp.Data;
using MessagingApp.Models.Domain;
using MessagingApp.Models.ViewModels;
using MessagingApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MessagingApp.Services
{
    public class FriendService : IFriendService
    {
        private readonly AppDbContext _db;

        public FriendService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<FriendViewModel>> GetFriendsAsync(string userId)
        {
            var friendships = await _db.Friendships
                .Where(f => f.UserId == userId || f.FriendId == userId)
                .Include(f => f.User)
                .Include(f => f.Friend)
                .ToListAsync();

            return friendships.Select(f =>
            {
                var friend = f.UserId == userId ? f.Friend : f.User;
                return new FriendViewModel
                {
                    Id = friend.Id,
                    FullName = $"{friend.FirstName} {friend.LastName}",
                    ProfilePicture = friend.ProfilePicture,
                    Bio = friend.Bio
                };
            }).ToList();
        }

        public async Task<List<FriendRequestViewModel>> GetPendingRequestsAsync(string userId)
        {
            return await _db.FriendRequests
                .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
                .Include(r => r.Sender)
                .Select(r => new FriendRequestViewModel
                {
                    Id = r.Id,
                    SenderId = r.SenderId,
                    SenderName = $"{r.Sender.FirstName} {r.Sender.LastName}",
                    SenderProfilePicture = r.Sender.ProfilePicture,
                    SentAt = r.SentAt
                })
                .ToListAsync();
        }

        public async Task<List<FriendViewModel>> SearchUsersAsync(string query, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<FriendViewModel>();

            query = query.ToLower();

            return await _db.Users
                .Where(u => u.Id != currentUserId &&
                    (
                        u.FirstName.ToLower().Contains(query) ||
                        u.LastName.ToLower().Contains(query) ||
                        u.Email!.ToLower().Contains(query)
                    ))
                .Select(u => new FriendViewModel
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    ProfilePicture = u.ProfilePicture,
                    Bio = u.Bio,

                    // ✅ Already friend
                    IsFriend = _db.Friendships.Any(f =>
                        (f.UserId == currentUserId && f.FriendId == u.Id) ||
                        (f.UserId == u.Id && f.FriendId == currentUserId)
                    ),

                    // ✅ Request sent
                    IsRequestSent = _db.FriendRequests.Any(r =>
                        r.SenderId == currentUserId &&
                        r.ReceiverId == u.Id &&
                        r.Status == FriendRequestStatus.Pending
                    )
                })
                .Take(20)
                .ToListAsync();
        }

        public async Task<bool> SendFriendRequestAsync(string senderId, string receiverId)
        {
            var exists = await _db.FriendRequests.AnyAsync(r =>
                ((r.SenderId == senderId && r.ReceiverId == receiverId) ||
                 (r.SenderId == receiverId && r.ReceiverId == senderId)) &&
                r.Status == FriendRequestStatus.Pending);

            if (exists) return false;

            var alreadyFriends = await AreFriendsAsync(senderId, receiverId);
            if (alreadyFriends) return false;

            _db.FriendRequests.Add(new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = FriendRequestStatus.Pending,
                SentAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AcceptFriendRequestAsync(int requestId, string currentUserId)
        {
            var request = await _db.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId);

            if (request == null) return false;

            request.Status = FriendRequestStatus.Accepted;

            _db.Friendships.Add(new Friendship
            {
                UserId = request.SenderId,
                FriendId = request.ReceiverId,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectFriendRequestAsync(int requestId, string currentUserId)
        {
            var request = await _db.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId);

            if (request == null) return false;

            request.Status = FriendRequestStatus.Rejected;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AreFriendsAsync(string userId1, string userId2)
        {
            return await _db.Friendships.AnyAsync(f =>
                (f.UserId == userId1 && f.FriendId == userId2) ||
                (f.UserId == userId2 && f.FriendId == userId1)
                );
        }
       
        public async Task<bool> RemoveFriendAsync(string currentUserId, string friendId)
        {
            
            var friendship = await _db.Friendships
            .FirstOrDefaultAsync(f =>
            (f.UserId == currentUserId && f.FriendId == friendId) ||
            (f.UserId == friendId && f.FriendId == currentUserId));
            if (friendship == null) return false;
            if (friendship != null)
            {
                _db.Friendships.Remove(friendship);
                await _db.SaveChangesAsync();
            }
            // (Optional but recommended) FriendRequest clean করা

            var request = await _db.FriendRequests
                .FirstOrDefaultAsync(r =>
                    (r.SenderId == currentUserId && r.ReceiverId == friendId) ||
                    (r.SenderId == friendId && r.ReceiverId == currentUserId));

            if (request != null)
            {
                _db.FriendRequests.Remove(request);
            }

            await _db.SaveChangesAsync();
            return true;

        }
        public async Task<List<FriendViewModel>> GetSuggestedFriendsAsync(string currentUserId)
        {
            var friendIds = await _db.Friendships
                .Where(f => f.UserId == currentUserId || f.FriendId == currentUserId)
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            var pendingRequestIds = await _db.FriendRequests
                .Where(r => (r.SenderId == currentUserId || r.ReceiverId == currentUserId) &&
                            r.Status == FriendRequestStatus.Pending)
                .Select(r => r.SenderId == currentUserId ? r.ReceiverId : r.SenderId)
                .ToListAsync();

            return await _db.Users
                .Where(u => u.Id != currentUserId &&
                            !friendIds.Contains(u.Id) &&
                            !pendingRequestIds.Contains(u.Id))
                .Select(u => new FriendViewModel
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    ProfilePicture = u.ProfilePicture,
                    Bio = u.Bio,
                    IsFriend = false,
                    IsRequestSent = false
                })
                .Take(10)
                .ToListAsync();
        }

    }
}
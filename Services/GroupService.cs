using MessagingApp.Data;
using MessagingApp.Models.Domain;
using MessagingApp.Models.ViewModels;
using MessagingApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MessagingApp.Services
{
    public class GroupService : IGroupService
    {
        private readonly AppDbContext _db;

        public GroupService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<int> CreateGroupAsync(string creatorId, string name, List<string> memberIds)
        {
            var group = new Group
            {
                Name = name,
                CreatedById = creatorId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Groups.Add(group);
            await _db.SaveChangesAsync();

            _db.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = creatorId,
                Role = GroupRole.Admin,
                JoinedAt = DateTime.UtcNow
            });

            foreach (var memberId in memberIds.Where(id => id != creatorId))
            {
                _db.GroupMembers.Add(new GroupMember
                {
                    GroupId = group.Id,
                    UserId = memberId,
                    Role = GroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return group.Id;
        }

        public async Task<GroupChatViewModel?> GetGroupChatAsync(int groupId, string currentUserId)
        {
            var group = await _db.Groups
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Include(g => g.Messages.OrderBy(msg => msg.SentAt).Take(50))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return null;

            var currentMember = group.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentMember == null) return null;

            return new GroupChatViewModel
            {
                GroupId = group.Id,
                GroupName = group.Name,
                CurrentUserId = currentUserId,
                IsAdmin = currentMember.Role == GroupRole.Admin,
                Messages = group.Messages.Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                    Content = m.IsDeleted ? "This message was deleted" : m.Content,
                    SentAt = m.SentAt,
                    EditedAt = m.EditedAt,
                    IsDeleted = m.IsDeleted,
                    IsMine = m.SenderId == currentUserId
                }).ToList(),
                Members = group.Members.Select(m => new GroupMemberViewModel
                {
                    UserId = m.UserId,
                    FullName = $"{m.User.FirstName} {m.User.LastName}",
                    ProfilePicture = m.User.ProfilePicture,
                    Role = m.Role.ToString()
                }).ToList()
            };
        }

        public async Task<ManageGroupViewModel?> GetManageGroupAsync(int groupId, string currentUserId)
        {
            var group = await _db.Groups
                .Include(g => g.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return null;

            var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupRole.Admin);
            if (!isAdmin) return null;

            var memberIds = group.Members.Select(m => m.UserId).ToList();

            var friendsNotInGroup = await _db.Friendships
                .Where(f => (f.UserId == currentUserId || f.FriendId == currentUserId))
                .Include(f => f.User)
                .Include(f => f.Friend)
                .ToListAsync();

            var friendItems = friendsNotInGroup.Select(f =>
            {
                var friend = f.UserId == currentUserId ? f.Friend : f.User;
                return new FriendSelectItem
                {
                    Id = friend.Id,
                    FullName = $"{friend.FirstName} {friend.LastName}",
                    ProfilePicture = friend.ProfilePicture
                };
            })
            .Where(f => !memberIds.Contains(f.Id))
            .ToList();

            return new ManageGroupViewModel
            {
                GroupId = group.Id,
                GroupName = group.Name,
                Members = group.Members.Select(m => new GroupMemberViewModel
                {
                    UserId = m.UserId,
                    FullName = $"{m.User.FirstName} {m.User.LastName}",
                    ProfilePicture = m.User.ProfilePicture,
                    Role = m.Role.ToString()
                }).ToList(),
                FriendsNotInGroup = friendItems
            };
        }

        public async Task<bool> AddMemberAsync(int groupId, string userId, string adminId)
        {
            var isAdmin = await IsGroupAdminAsync(groupId, adminId);
            if (!isAdmin) return false;

            var alreadyMember = await _db.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
            if (alreadyMember) return false;

            _db.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(int groupId, string userId, string adminId)
        {
            var isAdmin = await IsGroupAdminAsync(groupId, adminId);
            if (!isAdmin) return false;

            if (userId == adminId) return false;

            var member = await _db.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
            if (member == null) return false;

            _db.GroupMembers.Remove(member);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<MessageViewModel> SaveGroupMessageAsync(int groupId, string senderId, string content)
        {
            var message = new GroupMessage
            {
                GroupId = groupId,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _db.GroupMessages.Add(message);
            await _db.SaveChangesAsync();

            var sender = await _db.Users.FindAsync(senderId);

            return new MessageViewModel
            {
                Id = message.Id,
                SenderId = senderId,
                SenderName = $"{sender!.FirstName} {sender.LastName}",
                Content = content,
                SentAt = message.SentAt,
                IsDeleted = false,
                IsMine = true
            };
        }

        public async Task<MessageViewModel?> EditGroupMessageAsync(int messageId, string userId, string newContent)
        {
            var message = await _db.GroupMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null || message.IsDeleted) return null;

            message.Content = newContent;
            message.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new MessageViewModel
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = $"{message.Sender.FirstName} {message.Sender.LastName}",
                Content = message.Content,
                SentAt = message.SentAt,
                EditedAt = message.EditedAt,
                IsDeleted = false,
                IsMine = true
            };
        }

        public async Task<bool> DeleteGroupMessageAsync(int messageId, string userId)
        {
            var message = await _db.GroupMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null) return false;

            message.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<GroupPreviewViewModel>> GetGroupPreviewsAsync(string userId)
        {
            return await _db.GroupMembers
                .Where(m => m.UserId == userId)
                .Include(m => m.Group)
                    .ThenInclude(g => g.Messages.OrderByDescending(msg => msg.SentAt).Take(1))
                .Include(m => m.Group)
                    .ThenInclude(g => g.Members)
                .Select(m => new GroupPreviewViewModel
                {
                    GroupId = m.Group.Id,
                    GroupName = m.Group.Name,
                    LastMessage = m.Group.Messages.Any()
                        ? (m.Group.Messages.First().IsDeleted ? "Message deleted" : m.Group.Messages.First().Content)
                        : null,
                    LastMessageTime = m.Group.Messages.Any() ? m.Group.Messages.First().SentAt : null,
                    MemberCount = m.Group.Members.Count
                })
                .ToListAsync();
        }

        public async Task<bool> IsGroupAdminAsync(int groupId, string userId)
        {
            return await _db.GroupMembers.AnyAsync(m =>
                m.GroupId == groupId && m.UserId == userId && m.Role == GroupRole.Admin);
        }

        public async Task<bool> IsMemberAsync(int groupId, string userId)
        {
            return await _db.GroupMembers.AnyAsync(m =>
                m.GroupId == groupId && m.UserId == userId);
        }
    }
}
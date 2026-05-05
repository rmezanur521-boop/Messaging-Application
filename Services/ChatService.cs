using MessagingApp.Data;
using MessagingApp.Models.Domain;
using MessagingApp.Models.ViewModels;
using MessagingApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MessagingApp.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;

        public ChatService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<MessageViewModel>> GetConversationAsync(string userId1, string userId2, string currentUserId)
        {
            return await _db.Messages
                .Where(m =>
                    (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                    (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderBy(m => m.SentAt)
                .Take(50)
                .Include(m => m.Sender)
                .Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                    Content = m.IsDeleted ? "This message was deleted" : m.Content,
                    SentAt = m.SentAt,
                    EditedAt = m.EditedAt,
                    IsDeleted = m.IsDeleted,
                    IsMine = m.SenderId == currentUserId
                })
                .ToListAsync();
        }

        public async Task<MessageViewModel> SaveMessageAsync(string senderId, string receiverId, string content)
        {
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
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

        public async Task<MessageViewModel?> EditMessageAsync(int messageId, string userId, string newContent)
        {
            var message = await _db.Messages
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

        public async Task<bool> DeleteMessageAsync(int messageId, string userId)
        {
            var message = await _db.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null) return false;

            message.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<ChatPreviewViewModel>> GetChatPreviewsAsync(string userId)
        {
            var messages = await _db.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var last = g.First();
                    var friend = last.SenderId == userId ? last.Receiver : last.Sender;
                    return new ChatPreviewViewModel
                    {
                        FriendId = friend.Id,
                        FriendName = $"{friend.FirstName} {friend.LastName}",
                        FriendProfilePicture = friend.ProfilePicture,
                        LastMessage = last.IsDeleted ? "Message deleted" : last.Content,
                        LastMessageTime = last.SentAt
                    };
                })
                .ToList();

            return conversations;
        }
    }
}
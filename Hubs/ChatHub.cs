using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MessagingApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IGroupService _groupService;
        private readonly IFriendService _friendService;

        private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

        public ChatHub(IChatService chatService, IGroupService groupService, IFriendService friendService)
        {
            _chatService = chatService;
            _groupService = groupService;
            _friendService = friendService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier!;
            OnlineUsers[userId] = Context.ConnectionId;
            await Clients.Others.SendAsync("UserOnline", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier!;
            OnlineUsers.TryRemove(userId, out _);
            await Clients.Others.SendAsync("UserOffline", userId);
            await base.OnDisconnectedAsync(exception);
        }

        // ── Direct Messages ──────────────────────────────────────

        public async Task SendMessage(string receiverId, string content)
        {
            var senderId = Context.UserIdentifier!;

            if (string.IsNullOrWhiteSpace(content)) return;

            bool areFriends = await _friendService.AreFriendsAsync(senderId, receiverId);
            if (!areFriends) return;

            var message = await _chatService.SaveMessageAsync(senderId, receiverId, content);

            await Clients.User(senderId).SendAsync("ReceiveMessage", message);
            await Clients.User(receiverId).SendAsync("ReceiveMessage", message);
        }

        public async Task EditMessage(int messageId, string newContent)
        {
            var userId = Context.UserIdentifier!;

            if (string.IsNullOrWhiteSpace(newContent)) return;

            var message = await _chatService.EditMessageAsync(messageId, userId, newContent);
            if (message == null) return;

            await Clients.User(userId).SendAsync("MessageEdited", message);
        }

        public async Task DeleteMessage(int messageId, string receiverId)
        {
            var userId = Context.UserIdentifier!;

            var success = await _chatService.DeleteMessageAsync(messageId, userId);
            if (!success) return;

            await Clients.User(userId).SendAsync("MessageDeleted", messageId);
            await Clients.User(receiverId).SendAsync("MessageDeleted", messageId);
        }

        public async Task SendTyping(string receiverId)
        {
            var userId = Context.UserIdentifier!;
            await Clients.User(receiverId).SendAsync("UserTyping", userId);
        }

        public async Task StopTyping(string receiverId)
        {
            var userId = Context.UserIdentifier!;
            await Clients.User(receiverId).SendAsync("UserStoppedTyping", userId);
        }

        // ── Group Messages ───────────────────────────────────────

        public async Task JoinGroup(int groupId)
        {
            var userId = Context.UserIdentifier!;
            bool isMember = await _groupService.IsMemberAsync(groupId, userId);
            if (!isMember) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        }

        public async Task SendGroupMessage(int groupId, string content)
        {
            var senderId = Context.UserIdentifier!;

            if (string.IsNullOrWhiteSpace(content)) return;

            bool isMember = await _groupService.IsMemberAsync(groupId, senderId);
            if (!isMember) return;

            var message = await _groupService.SaveGroupMessageAsync(groupId, senderId, content);

            await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", message);
        }

        public async Task EditGroupMessage(int messageId, int groupId, string newContent)
        {
            var userId = Context.UserIdentifier!;

            if (string.IsNullOrWhiteSpace(newContent)) return;

            var message = await _groupService.EditGroupMessageAsync(messageId, userId, newContent);
            if (message == null) return;

            await Clients.Group($"group_{groupId}").SendAsync("GroupMessageEdited", message);
        }

        public async Task DeleteGroupMessage(int messageId, int groupId)
        {
            var userId = Context.UserIdentifier!;

            var success = await _groupService.DeleteGroupMessageAsync(messageId, userId);
            if (!success) return;

            await Clients.Group($"group_{groupId}").SendAsync("GroupMessageDeleted", messageId);
        }

        public async Task SendGroupTyping(int groupId)
        {
            var userId = Context.UserIdentifier!;
            await Clients.OthersInGroup($"group_{groupId}").SendAsync("GroupUserTyping", userId);
        }

        public async Task StopGroupTyping(int groupId)
        {
            var userId = Context.UserIdentifier!;
            await Clients.OthersInGroup($"group_{groupId}").SendAsync("GroupUserStoppedTyping", userId);
        }

        public static bool IsOnline(string userId) => OnlineUsers.ContainsKey(userId);
    }
}
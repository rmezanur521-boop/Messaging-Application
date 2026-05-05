using MessagingApp.Models.Domain;
using MessagingApp.Models.ViewModels;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IFriendService _friendService;
        private readonly IGroupService _groupService;
        private readonly UserManager<AppUser> _userManager;

        public ChatController(
            IChatService chatService,
            IFriendService friendService,
            IGroupService groupService,
            UserManager<AppUser> userManager)
        {
            _chatService = chatService;
            _friendService = friendService;
            _groupService = groupService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var vm = new ChatViewModel
            {
                Conversations = await _chatService.GetChatPreviewsAsync(userId),
                Groups = await _groupService.GetGroupPreviewsAsync(userId)
            };

            return View(vm);
        }

        public async Task<IActionResult> Conversation(string friendId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            bool areFriends = await _friendService.AreFriendsAsync(currentUser.Id, friendId);
            if (!areFriends) return RedirectToAction("Index");

            var friend = await _userManager.FindByIdAsync(friendId);
            if (friend == null) return RedirectToAction("Index");

            var messages = await _chatService.GetConversationAsync(currentUser.Id, friendId, currentUser.Id);

            var vm = new ConversationViewModel
            {
                FriendId = friendId,
                FriendName = $"{friend.FirstName} {friend.LastName}",
                FriendProfilePicture = friend.ProfilePicture,
                CurrentUserId = currentUser.Id,
                Messages = messages
            };

            return View(vm);
        }
    }
}
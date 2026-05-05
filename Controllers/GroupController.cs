using MessagingApp.Models.Domain;
using MessagingApp.Models.ViewModels;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly IGroupService _groupService;
        private readonly IFriendService _friendService;
        private readonly UserManager<AppUser> _userManager;

        public GroupController(
            IGroupService groupService,
            IFriendService friendService,
            UserManager<AppUser> userManager)
        {
            _groupService = groupService;
            _friendService = friendService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User)!;
            var friends = await _friendService.GetFriendsAsync(userId);

            var vm = new CreateGroupViewModel
            {
                Friends = friends.Select(f => new FriendSelectItem
                {
                    Id = f.Id,
                    FullName = f.FullName,
                    ProfilePicture = f.ProfilePicture
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Group name is required");
                var friends = await _friendService.GetFriendsAsync(userId);
                model.Friends = friends.Select(f => new FriendSelectItem
                {
                    Id = f.Id,
                    FullName = f.FullName,
                    ProfilePicture = f.ProfilePicture
                }).ToList();
                return View(model);
            }

            var groupId = await _groupService.CreateGroupAsync(userId, model.Name, model.SelectedFriendIds);
            return RedirectToAction("Chat", new { groupId });
        }

        public async Task<IActionResult> Chat(int groupId)
        {
            var userId = _userManager.GetUserId(User)!;
            var vm = await _groupService.GetGroupChatAsync(groupId, userId);

            if (vm == null) return RedirectToAction("Index", "Chat");

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Manage(int groupId)
        {
            var userId = _userManager.GetUserId(User)!;
            var vm = await _groupService.GetManageGroupAsync(groupId, userId);

            if (vm == null) return RedirectToAction("Index", "Chat");

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int groupId, string userId)
        {
            var adminId = _userManager.GetUserId(User)!;
            await _groupService.AddMemberAsync(groupId, userId, adminId);
            return RedirectToAction("Manage", new { groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int groupId, string userId)
        {
            var adminId = _userManager.GetUserId(User)!;
            await _groupService.RemoveMemberAsync(groupId, userId, adminId);
            return RedirectToAction("Manage", new { groupId });
        }
    }
}
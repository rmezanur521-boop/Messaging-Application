using MessagingApp.Models.Domain;
using MessagingApp.Models.ViewModels;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IFriendService _friendService;
        private readonly IFileService _fileService;

        public ProfileController(UserManager<AppUser> userManager, IFriendService friendService, IFileService fileService)
        {
            _userManager = userManager;
            _friendService = friendService;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index(string? id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var targetId = id ?? currentUser.Id;
            var targetUser = await _userManager.FindByIdAsync(targetId);
            if (targetUser == null) return NotFound();

            bool isFriend = false;
            bool hasPendingRequest = false;
            bool requestSentByMe = false;
            int friendRequestId = 0;

            if (targetId != currentUser.Id)
            {
                isFriend = await _friendService.AreFriendsAsync(currentUser.Id, targetId);

                if (!isFriend)
                {
                    var pendingRequests = await _friendService.GetPendingRequestsAsync(currentUser.Id);
                    var sentRequest = pendingRequests.FirstOrDefault(r => r.SenderId == targetId);

                    if (sentRequest != null)
                    {
                        hasPendingRequest = true;
                        requestSentByMe = false;
                        friendRequestId = sentRequest.Id;
                    }
                }
            }

            var vm = new ProfileViewModel
            {
                Id = targetUser.Id,
                FirstName = targetUser.FirstName,
                LastName = targetUser.LastName,
                Email = targetUser.Email ?? string.Empty,
                DateOfBirth = targetUser.DateOfBirth,
                Gender = targetUser.Gender,
                Bio = targetUser.Bio,
                ProfilePicture = _fileService.GetProfilePictureUrl(targetUser.ProfilePicture),
                CreatedAt = targetUser.CreatedAt,
                IsFriend = isFriend,
                HasPendingRequest = hasPendingRequest,
                RequestSentByMe = requestSentByMe,
                FriendRequestId = friendRequestId
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var vm = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Bio = user.Bio,
                ExistingProfilePicture = _fileService.GetProfilePictureUrl(user.ProfilePicture)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.Bio = model.Bio;

            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                await _fileService.DeleteProfilePictureAsync(user.ProfilePicture);
                var fileName = await _fileService.SaveProfilePictureAsync(model.ProfilePictureFile);
                if (fileName != null)
                    user.ProfilePicture = fileName;
            }

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profile updated successfully";
            return RedirectToAction("Index");
        }
    }
}
using MessagingApp.Data;
using MessagingApp.Models.Domain;
using MessagingApp.Models.ViewModels;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MessagingApp.Controllers
{
    [Authorize]
    public class FriendController : Controller
    {
        private readonly IFriendService _friendService;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public FriendController(IFriendService friendService,AppDbContext context, UserManager<AppUser> userManager)
        {
            _friendService = friendService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var vm = new FriendIndexViewModel
            {
                Friends = await _friendService.GetFriendsAsync(userId),
                PendingRequests = await _friendService.GetPendingRequestsAsync(userId)
            };

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> Search(string? query)
        {
            var userId = _userManager.GetUserId(User)!;

            var vm = new FriendIndexViewModel
            {
                Friends = await _friendService.GetFriendsAsync(userId),
                PendingRequests = await _friendService.GetPendingRequestsAsync(userId),
                SearchQuery = query,

                SearchResults = string.IsNullOrWhiteSpace(query)
                    ? await _context.Users
                        .Where(x => x.Id != userId)
                        .Take(40)
                        .Select(x => new FriendViewModel
                        {
                            Id = x.Id,
                            FullName = x.FirstName + " " + x.LastName,
                            Bio = x.Bio,
                            ProfilePicture = x.ProfilePicture,

                            IsFriend = _context.Friendships.Any(f =>
                                (f.UserId == userId && f.FriendId == x.Id) ||
                                (f.UserId == x.Id && f.FriendId == userId)
                            ),
                            IsRequestSent = _context.FriendRequests.Any(r =>
                                r.SenderId == userId &&
                                r.ReceiverId == x.Id &&
                                r.Status == FriendRequestStatus.Pending
                            )
                        })
                        .ToListAsync()

                    : await _friendService.SearchUsersAsync(query, userId)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(string receiverId)
        {
            var userId = _userManager.GetUserId(User)!;

            await _friendService.SendFriendRequestAsync(userId, receiverId);

            TempData["Success"] = "Friend request sent";

            return RedirectToAction("Search");
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int requestId)
        {
            var userId = _userManager.GetUserId(User)!;
            await _friendService.AcceptFriendRequestAsync(requestId, userId);
            TempData["Success"] = "Friend request accepted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int requestId)
        {
            var userId = _userManager.GetUserId(User)!;
            await _friendService.RejectFriendRequestAsync(requestId, userId);
            TempData["Info"] = "Friend request rejected";
            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var userId = _userManager.GetUserId(User)!;

            await _friendService.RemoveFriendAsync(userId, friendId);

            TempData["Info"] = "Friend removed successfully";
            return RedirectToAction("Index");
        }
    }
}
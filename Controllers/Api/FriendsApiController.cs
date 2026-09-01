using MessagingApp.Models.DTOs.Common;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MessagingApp.Controllers.Api
{
    [ApiController]
    [Route("api/friends")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class FriendsApiController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendsApiController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        private string? GetCurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: api/friends
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var friends = await _friendService.GetFriendsAsync(currentUserId);
            return Ok(ApiResponse<object>.Ok(friends));
        }

        // GET: api/friends/requests
        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var requests = await _friendService.GetPendingRequestsAsync(currentUserId);
            return Ok(ApiResponse<object>.Ok(requests));
        }

        // GET: api/friends/search?query=john
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(ApiResponse<string>.Fail("Search query is required"));

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var users = await _friendService.SearchUsersAsync(query, currentUserId);
            return Ok(ApiResponse<object>.Ok(users));
        }

        // POST: api/friends/request/{receiverId}
        [HttpPost("request/{receiverId}")]
        public async Task<IActionResult> SendFriendRequest(string receiverId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            if (currentUserId == receiverId)
                return BadRequest(ApiResponse<string>.Fail("Cannot send request to yourself"));

            var result = await _friendService.SendFriendRequestAsync(currentUserId, receiverId);
            if (!result)
                return BadRequest(ApiResponse<string>.Fail("Request already sent or already friends"));

            return Ok(ApiResponse<string>.Ok("Sent", "Friend request sent"));
        }

        // PUT: api/friends/accept/{requestId}
        [HttpPut("accept/{requestId}")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _friendService.AcceptFriendRequestAsync(requestId, currentUserId);
            if (!result)
                return NotFound(ApiResponse<string>.Fail("Request not found or unauthorized"));

            return Ok(ApiResponse<string>.Ok("Accepted", "Friend request accepted"));
        }

        // PUT: api/friends/reject/{requestId}
        [HttpPut("reject/{requestId}")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _friendService.RejectFriendRequestAsync(requestId, currentUserId);
            if (!result)
                return NotFound(ApiResponse<string>.Fail("Request not found or unauthorized"));

            return Ok(ApiResponse<string>.Ok("Rejected", "Friend request rejected"));
        }

        // DELETE: api/friends/remove/{friendId}
        [HttpDelete("remove/{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _friendService.RemoveFriendAsync(currentUserId, friendId);
            if (!result)
                return NotFound(ApiResponse<string>.Fail("Friend not found"));

            return Ok(ApiResponse<string>.Ok("Removed", "Friend removed"));
        }

        [HttpGet("suggested")]
        public async Task<IActionResult> GetSuggestedFriends()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var suggestions = await _friendService.GetSuggestedFriendsAsync(currentUserId);
            return Ok(ApiResponse<object>.Ok(suggestions));
        }
        [HttpGet("request-status/{userId}")]
        public async Task<IActionResult> GetRequestStatus(string userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var status = await _friendService.GetRequestStatusAsync(currentUserId, userId);
            return Ok(ApiResponse<object>.Ok(new { userId, status }));
        }
    }
}
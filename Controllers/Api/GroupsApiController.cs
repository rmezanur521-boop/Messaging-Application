using MessagingApp.Models.DTOs.Common;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MessagingApp.Controllers.Api
{
    [ApiController]
    [Route("api/groups")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class GroupsApiController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupsApiController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        private string? GetCurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: api/groups/previews
        [HttpGet("previews")]
        public async Task<IActionResult> GetGroupPreviews()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var groups = await _groupService.GetGroupPreviewsAsync(currentUserId);
            return Ok(ApiResponse<object>.Ok(groups));
        }

        // GET: api/groups/{groupId}
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroupChat(int groupId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var isMember = await _groupService.IsMemberAsync(groupId, currentUserId);
            if (!isMember)
                return Forbid();

            var group = await _groupService.GetGroupChatAsync(groupId, currentUserId);
            if (group == null)
                return NotFound(ApiResponse<string>.Fail("Group not found"));

            return Ok(ApiResponse<object>.Ok(group));
        }

        // POST: api/groups/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid input"));

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var groupId = await _groupService
                .CreateGroupAsync(currentUserId, request.Name, request.MemberIds);
            return Ok(ApiResponse<object>.Ok(new { groupId }, "Group created"));
        }

        // POST: api/groups/{groupId}/messages/send
        [HttpPost("{groupId}/messages/send")]
        public async Task<IActionResult> SendGroupMessage(
            int groupId, [FromBody] SendGroupMessageDto request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var isMember = await _groupService.IsMemberAsync(groupId, currentUserId);
            if (!isMember)
                return Forbid();

            var message = await _groupService
                .SaveGroupMessageAsync(groupId, currentUserId, request.Content);
            return Ok(ApiResponse<object>.Ok(message, "Message sent"));
        }

        // PUT: api/groups/{groupId}/messages/edit/{messageId}
        [HttpPut("{groupId}/messages/edit/{messageId}")]
        public async Task<IActionResult> EditGroupMessage(
            int groupId, int messageId, [FromBody] EditGroupMessageDto request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _groupService
                .EditGroupMessageAsync(messageId, currentUserId, request.NewContent);
            if (result == null)
                return NotFound(ApiResponse<string>.Fail("Message not found or unauthorized"));

            return Ok(ApiResponse<object>.Ok(result, "Message updated"));
        }

        // DELETE: api/groups/{groupId}/messages/delete/{messageId}
        [HttpDelete("{groupId}/messages/delete/{messageId}")]
        public async Task<IActionResult> DeleteGroupMessage(int groupId, int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _groupService.DeleteGroupMessageAsync(messageId, currentUserId);
            if (!result)
                return NotFound(ApiResponse<string>.Fail("Message not found or unauthorized"));

            return Ok(ApiResponse<string>.Ok("Deleted", "Message deleted"));
        }

        // POST: api/groups/{groupId}/members/add/{userId}
        [HttpPost("{groupId}/members/add/{userId}")]
        public async Task<IActionResult> AddMember(int groupId, string userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var isAdmin = await _groupService.IsGroupAdminAsync(groupId, currentUserId);
            if (!isAdmin)
                return Forbid();

            var result = await _groupService.AddMemberAsync(groupId, userId, currentUserId);
            if (!result)
                return BadRequest(ApiResponse<string>.Fail("Could not add member"));

            return Ok(ApiResponse<string>.Ok("Added", "Member added"));
        }

        // DELETE: api/groups/{groupId}/members/remove/{userId}
        [HttpDelete("{groupId}/members/remove/{userId}")]
        public async Task<IActionResult> RemoveMember(int groupId, string userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var isAdmin = await _groupService.IsGroupAdminAsync(groupId, currentUserId);
            if (!isAdmin)
                return Forbid();

            var result = await _groupService.RemoveMemberAsync(groupId, userId, currentUserId);
            if (!result)
                return NotFound(ApiResponse<string>.Fail("Member not found"));

            return Ok(ApiResponse<string>.Ok("Removed", "Member removed"));
        }
    }

    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public List<string> MemberIds { get; set; } = new();
    }

    public class SendGroupMessageDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class EditGroupMessageDto
    {
        public string NewContent { get; set; } = string.Empty;
    }
}
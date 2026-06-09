using MessagingApp.Models.DTOs.Common;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MessagingApp.Controllers.Api
{
    [ApiController]
    [Route("api/messages")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class MessagesApiController : ControllerBase
    {
        private readonly IChatService _chatService;

        public MessagesApiController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private string? GetCurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: api/messages/previews
        [HttpGet("previews")]
        public async Task<IActionResult> GetChatPreviews()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var previews = await _chatService.GetChatPreviewsAsync(currentUserId);
            return Ok(ApiResponse<object>.Ok(previews));
        }

        // GET: api/messages/conversation/{userId}
        [HttpGet("conversation/{userId}")]
        public async Task<IActionResult> GetConversation(string userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var messages = await _chatService
                .GetConversationAsync(userId, currentUserId, currentUserId);
            return Ok(ApiResponse<object>.Ok(messages));
        }

        // POST: api/messages/send
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid input"));

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var message = await _chatService
                .SaveMessageAsync(currentUserId, request.ReceiverId, request.Content);
            return Ok(ApiResponse<object>.Ok(message, "Message sent"));
        }

        // PUT: api/messages/edit/{messageId}
        [HttpPut("edit/{messageId}")]
        public async Task<IActionResult> EditMessage(
            int messageId, [FromBody] EditMessageDto request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _chatService
                .EditMessageAsync(messageId, currentUserId, request.NewContent);
            if (result == null)
                return NotFound(ApiResponse<string>.Fail("Message not found or unauthorized"));

            return Ok(ApiResponse<object>.Ok(result, "Message updated"));
        }

        // DELETE: api/messages/delete/{messageId}
        [HttpDelete("delete/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _chatService.DeleteMessageAsync(messageId, currentUserId);
            if (!result)
                return NotFound(ApiResponse<string>.Fail("Message not found or unauthorized"));

            return Ok(ApiResponse<string>.Ok("Deleted", "Message deleted"));
        }
    }

    public class SendMessageDto
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class EditMessageDto
    {
        public string NewContent { get; set; } = string.Empty;
    }
}
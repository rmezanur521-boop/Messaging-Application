using MessagingApp.Models.DTOs.Common;
using MessagingApp.Models.Domain;
using MessagingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MessagingApp.Controllers.Api
{
    [ApiController]
    [Route("api/profile")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProfileApiController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IFileService _fileService;

        public ProfileApiController(
            UserManager<AppUser> userManager,
            IFileService fileService)
        {
            _userManager = userManager;
            _fileService = fileService;
        }

        private string? GetCurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return NotFound();

            return Ok(ApiResponse<object>.Ok(MapToDto(user)));
        }

        // GET: api/profile/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse<string>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(MapToDto(user)));
        }

        // PUT: api/profile/update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return NotFound();

            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.Bio = request.Bio ?? user.Bio;
            user.Gender = request.Gender ?? user.Gender;
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<string>.Fail(errors));
            }

            return Ok(ApiResponse<object>.Ok(MapToDto(user), "Profile updated"));
        }

        // POST: api/profile/picture
        // Flutter এ profile picture upload করতে
        [HttpPost("picture")]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<string>.Fail("No file provided"));

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return NotFound();
            _fileService.DeleteProfilePicture(user.ProfilePicture);

            var fileName = await _fileService.SaveProfilePictureAsync(file);
            if (fileName == null)
                return BadRequest(ApiResponse<string>.Fail("Failed to upload image"));

            user.ProfilePicture = fileName;
            await _userManager.UpdateAsync(user);

            return Ok(ApiResponse<object>.Ok(new { profilePicture = fileName }, "Picture updated"));
        }

        // Helper — Domain → DTO mapping
        private static object MapToDto(AppUser user) => new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Bio,
            user.Gender,
            user.DateOfBirth,
            user.ProfilePicture,
            user.CreatedAt
        };
    }

    public class UpdateProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
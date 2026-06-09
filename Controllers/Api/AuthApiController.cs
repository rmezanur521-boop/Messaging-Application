// Controllers/Api/AuthApiController.cs
using MessagingApp.Models.Domain;
using MessagingApp.Models.DTOs.Auth;
using MessagingApp.Models.DTOs.Common;
using MessagingApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MessagingApp.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly JwtService _jwtService;

        public AuthApiController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            JwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid input"));

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(ApiResponse<string>.Fail("Invalid credentials"));

            var result = await _signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
                return Unauthorized(ApiResponse<string>.Fail("Invalid credentials"));

            var token = _jwtService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Login successful"));
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid input"));

            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return BadRequest(ApiResponse<string>.Fail("Email already in use"));

            var user = new AppUser
            {
                UserName = request.UserName,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ",
                    result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse<string>.Fail(errors));
            }

            var token = _jwtService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Registration successful"));
        }

        // GET: api/auth/me  ← Flutter app startup-এ token validate করতে
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return NotFound();

            return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!
            }));
        }
    }
}
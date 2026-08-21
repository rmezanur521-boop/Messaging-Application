using MessagingApp.Data;
using MessagingApp.Models.Domain;
using MessagingApp.Models.DTOs.Auth;
using MessagingApp.Models.DTOs.Common;
using MessagingApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MessagingApp.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly JwtService _jwtService;
        private readonly AppDbContext _context;

        public AuthApiController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            JwtService jwtService,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _context = context;
        }

        // Login/Register এর পর access + refresh token একসাথে বানিয়ে DB তে save করে
        private async Task<AuthResponse> GenerateAuthResponseAsync(AppUser user)
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtService.GetRefreshTokenExpiryDays())
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshTokenValue,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtService.GetAccessTokenExpiryMinutes())
            };
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

            var response = await GenerateAuthResponseAsync(user);

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

            var response = await GenerateAuthResponseAsync(user);

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Registration successful"));
        }

        // POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (storedToken == null || !storedToken.IsActive)
                return Unauthorized(ApiResponse<string>.Fail("Invalid or expired refresh token"));
            storedToken.IsRevoked = true;

            var response = await GenerateAuthResponseAsync(storedToken.User);

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Token refreshed"));
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

            if (storedToken == null)
                return NotFound(ApiResponse<string>.Fail("Refresh token not found"));

            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok("", "Logged out successfully"));
        }

        // GET: api/auth/me
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
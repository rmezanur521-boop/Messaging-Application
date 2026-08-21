using System.ComponentModel.DataAnnotations;

namespace MessagingApp.Models.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
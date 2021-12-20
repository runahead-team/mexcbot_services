using System.ComponentModel.DataAnnotations;
using sp.Core.Constants;

namespace multexbot.Api.RequestModels.User
{
    public class RegisterRequest
    {
        [Required]
        [RegularExpression(AppConstants.UsernameRegex)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(32)]
        public string Password { get; set; }

        public long SponsorId { get; set; }
        
        public string SponsorUsername { get; set; }
        
        public string EmailOtp { get; set; }
        
        public string ReCaptcha { get; set; }
    }
}
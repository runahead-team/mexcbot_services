using System.ComponentModel.DataAnnotations;
using sp.Core.Constants;

namespace multexbot.Api.RequestModels.User
{
    public class LoginRequest
    {
        [Required]
        [RegularExpression(AppConstants.UsernameRegex)]
        public string Username { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(32)]
        public string Password { get; set; }

        public string GaCode { get; set; }
        
        public string ReCaptcha { get; set; }

    }
    
    public class AdminLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [MinLength(8)]
        [MaxLength(32)]
        public string Password { get; set; }
        
        public string GaCode { get; set; }
    }
}
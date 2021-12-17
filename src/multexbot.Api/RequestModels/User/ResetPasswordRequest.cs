using System.ComponentModel.DataAnnotations;
using sp.Core.Constants;

namespace multexBot.Api.RequestModels.User
{
    public class ResetPasswordRequest
    {
        [Required]
        [RegularExpression(AppConstants.UsernameRegex)]
        public string Username { get; set; }
        
        [Required]
        [MinLength(8)]
        [MaxLength(32)]
        public string Password { get; set; }
        
        [Required]
        public string EmailOtp { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using sp.Core.Constants;

namespace multexBot.Api.RequestModels.User
{
    public class SendRegisterOtpRequest
    {
        [Required]
        [RegularExpression(AppConstants.UsernameRegex)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        public string ReCaptcha { get; set; }
    }
}
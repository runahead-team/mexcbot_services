using System.ComponentModel.DataAnnotations;

namespace multexbot.Api.RequestModels.User
{
    public class ChangePasswordRequest
    {
        [Required]
        [MinLength(8)]
        [MaxLength(32)]
        public string OldPassword { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(32)]
        public string Password { get; set; }

    }
}
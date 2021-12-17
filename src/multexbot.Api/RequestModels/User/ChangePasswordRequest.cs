using System.ComponentModel.DataAnnotations;

namespace multexBot.Api.RequestModels.User
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
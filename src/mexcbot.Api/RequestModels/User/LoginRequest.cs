using System.ComponentModel.DataAnnotations;

namespace mexcbot.Api.RequestModels.User
{
    public class LoginRequest
    {
        public string Username { get; set; }
        
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }
    }
}
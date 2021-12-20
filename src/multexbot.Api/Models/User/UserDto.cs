using multexbot.Api.Constants;
using multexbot.Api.Models.Base;

namespace multexbot.Api.Models.User
{
    public class UserDto : BaseDto
    {
        public string Username { get; set; }
        
        public string Account { get; set; }

        public string Email { get; set; }

        public UserRank Rank { get; set; }
        
        public VerifyLevel VerifyLevel { get; set; }
        
        public UserStatus Status { get; set; }

        public bool? BlockWithdraw { get; set; }
        
    }
}
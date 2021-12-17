using multexBot.Api.Constants;
using multexBot.Api.Models.Base;

namespace multexBot.Api.Models.User
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
using multexBot.Api.Constants;
using multexBot.Api.Models.Base;

namespace multexBot.Api.ResponseModels.User
{
    public class GetMemberResponse : BaseDto
    {
        public string Username { get; set; }

        public string Email { get; set; }

        public UserStatus Status { get; set; }
        
        public int Level { get; set; }
    }
}
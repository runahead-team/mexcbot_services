using multexbot.Api.Constants;
using multexbot.Api.Models.Base;

namespace multexbot.Api.ResponseModels.User
{
    public class GetMemberResponse : BaseDto
    {
        public string Username { get; set; }

        public string Email { get; set; }

        public UserStatus Status { get; set; }
        
        public int Level { get; set; }
    }
}
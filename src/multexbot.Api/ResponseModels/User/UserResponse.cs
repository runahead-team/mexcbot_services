using multexbot.Api.Constants;
using multexbot.Api.Models.Base;

namespace multexbot.Api.ResponseModels.User
{
    public class UserResponse : BaseDto
    {
        public string Username { get; set; }

        public string Account { get; set; }
        
        public string AvatarImage { get; set; }
        
        public string CoverImage { get; set; }
        
        public UserRank Rank { get; set; }
        
        public VerifyLevel VerifyLevel { get; set; }
        
        public UserStatus Status { get; set; }

        public int MemberCount { get; set; }
        
        public int PostCount { get; set; }
        
        public int DonateCount { get; set; }

        public decimal TotalDonate { get; set; }
        
        public int FollowCount { get; set; }
    }
}
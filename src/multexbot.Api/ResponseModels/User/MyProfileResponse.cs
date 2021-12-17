using multexBot.Api.Constants;
using multexBot.Api.Models.Base;

namespace multexBot.Api.ResponseModels.User
{
    public class MyProfileResponse : BaseDto
    {
        public string Username { get; set; }

        public string Account { get; set; }
        
        public string AvatarImage { get; set; }
        
        public string CoverImage { get; set; }
        
        public UserRank Rank { get; set; }
        
        public VerifyLevel VerifyLevel { get; set; }
        
        public UserStatus Status { get; set; }

        public int MemberCount { get; set; }
        
        public bool GaEnable { get; set; }

        public long? PasswordUpdatedTime { get; set; }
        
        public bool? BlockWithdraw { get; set; }
        
        public int PostCount { get; set; }
        
        public int DonateCount { get; set; }

        public decimal TotalDonate { get; set; }
        
        public int FollowCount { get; set; }
        
    }
}
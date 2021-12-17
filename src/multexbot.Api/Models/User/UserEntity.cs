
using multexBot.Api.Constants;
using multexBot.Api.Models.Base;
using multexBot.Api.RequestModels.User;
using sp.Core.Utils;

namespace multexBot.Api.Models.User
{
    public class UserEntity : BaseEntity
    {
        public UserEntity()
        {
        }

        public UserEntity(RegisterRequest request)
        {
            Username = request.Username.ToLower();
            Email = request.Email.ToLower();
            Password = request.Password;
            VerifyLevel = VerifyLevel.NOT_VERIFY;
            Status = UserStatus.ACTIVE;
            CreatedTime = AppUtils.NowMilis();
        }

        public long? SponsorId { get; set; }
        
        public string Email { get; set; }
        
        public string Username { get; set; }

        public string Account { get; set; }
        
        public string AvatarImage { get; set; }
        
        public string CoverImage { get; set; }
        
        public UserRank Rank { get; set; }
        
        public VerifyLevel VerifyLevel { get; set; }
        
        public UserStatus Status { get; set; }
        
        public int MemberCount { get; set; }
        
        public string Password { get; set; }

        public string GaSecret { get; set; }

        public bool GaEnable { get; set; }

        public long? PasswordUpdatedTime { get; set; }
        
        public bool? BlockWithdraw { get; set; }

        public int PostCount { get; set; }
        
        public int DonateCount { get; set; }

        public decimal TotalDonate { get; set; }
        
        public int FollowCount { get; set; }
    }
}
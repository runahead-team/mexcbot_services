namespace multexBot.Api.Models.Referral
{
    public class DirectReferralEntity
    {
        public long UserId { get; set; }

        public long SponsorId { get; set; }

        public int Level { get; set; }

        public long CreatedTime { get; set; }
    }
}
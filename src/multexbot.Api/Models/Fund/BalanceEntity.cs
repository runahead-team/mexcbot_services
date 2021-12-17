namespace multexBot.Api.Models.Fund
{
    public class BalanceEntity
    {
        public long UserId { get; set; }

        public string Asset { get; set; }

        public decimal Amount { get; set; }

        public decimal BlockAmount { get; set; }

        public long CreatedTime { get; set; }
        
        public long UpdatedTime { get; set; }
        
        public int? BlockDays { get; set; }
        
        public long? LastUnblock { get; set; }

    }
}
using multexBot.Api.Constants;

namespace multexBot.Api.Models.Fund
{
    public class BalanceLogEntity
    {
        public long UserId { get; set; }

        public string Asset { get; set; }

        public TransactionType Type { get; set; }
        
        public decimal Amount { get; set; }

        public decimal BlockAmount { get; set; }

        public string Text { get; set; }

        public long CreatedTime { get; set; }
    }
}
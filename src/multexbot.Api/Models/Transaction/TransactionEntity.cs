using multexBot.Api.Constants;

namespace multexBot.Api.Models.Transaction
{
    public class TransactionEntity
    {
        
        public long Id { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public decimal Amount { get; set; }

        public string Asset { get; set; }

        public TransactionType Type { get; set; }
        
        public long CreatedTime { get; set; }

        public long? RefId { get; set; }

        public string Text { get; set; }
        
    }
}
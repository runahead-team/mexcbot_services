using multexBot.Api.Constants;

namespace multexBot.Api.Models.Fund
{
    public class DepositEntity
    {
        public long Id { get; set; }
        
        public string Guid { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }
        
        public string Asset { get; set; }

        public string Network { get; set; }

        public string Address { get; set; }

        public string AddressTag { get; set; }

        public decimal Amount { get; set; }
        
        public decimal UsdAmount { get; set; }

        public string TxId { get; set; }

        public long CreatedTime { get; set; }

        public string Sender { get; set; }

        public long? SenderId { get; set; }
    }
}
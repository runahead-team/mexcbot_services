namespace multexBot.Api.Models.Wallet
{
    public class WalletDeposit
    {
        public string Service { get; set; }

        public string Currency { get; set; }

        public string Address { get; set; }

        public string Memo { get; set; }
        
        public string TransactionHash { get; set; }

        public decimal Amount { get; set; }
    }
}
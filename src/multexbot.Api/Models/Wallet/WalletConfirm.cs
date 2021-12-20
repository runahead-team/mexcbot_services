namespace multexbot.Api.Models.Wallet
{
    public class WalletConfirm
    {
        public long ExternalId { get; set; }

        public string TransactionHash { get; set; }

        public string Status { get; set; } // success, failed
    }
}
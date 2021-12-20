using System.Collections.Generic;

namespace multexbot.Api.Models.Wallet
{
    public class WalletWithdrawRequest
    {
        public string Type { get; set; } = "withdrawal";

        public string Service { get; set; }

        public string Currency { get; set; }

        public List<WalletWithdrawRequestItem> Transactions { get; set; }
    }

    public class WalletWithdrawRequestItem
    {
        public long Id { get; set; }

        public decimal Amount { get; set; }

        public string Address { get; set; }

        public string Memo { get; set; }

        public string Currency { get; set; }
    }
}
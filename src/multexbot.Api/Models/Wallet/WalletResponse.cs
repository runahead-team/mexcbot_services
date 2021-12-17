namespace multexBot.Api.Models.Wallet
{
    public class WalletResponse<T>  where T : class
    {
        public bool Success { get; set; }

        public T Data { get; set; }

        public WalletError Error { get; set; }
    }

    public class WalletError
    {
        public string Code { get; set; }

        public string Message { get; set; }
    }
}
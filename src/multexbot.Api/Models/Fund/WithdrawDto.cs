namespace multexBot.Api.Models.Fund
{
    public class WithdrawDto : WithdrawEntity
    {
        public decimal Total => Amount + Fee;
    }
}
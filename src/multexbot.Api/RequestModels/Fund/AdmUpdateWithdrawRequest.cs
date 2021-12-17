using multexBot.Api.Constants;
using multexBot.Api.RequestModels.Common;

namespace multexBot.Api.RequestModels.Fund
{
    public class AdmUpdateWithdrawRequest : IdsRequest
    {
        public WithdrawStatus Status { get; set; }

        public string TxId { get; set; }
    }
}
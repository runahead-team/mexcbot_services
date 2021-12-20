using multexbot.Api.Constants;
using multexbot.Api.RequestModels.Common;

namespace multexbot.Api.RequestModels.Fund
{
    public class AdmUpdateWithdrawRequest : IdsRequest
    {
        public WithdrawStatus Status { get; set; }

        public string TxId { get; set; }
    }
}
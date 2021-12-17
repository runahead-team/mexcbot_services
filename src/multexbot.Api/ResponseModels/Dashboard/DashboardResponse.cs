using multexBot.Api.ResponseModels.User;

namespace multexBot.Api.ResponseModels.Dashboard
{
    public class DashboardResponse
    {
    }

    public class AdmDashboardResponse
    {
        #region Basic

        public int PendingKyc { get; set; }

        public int PendingWithdraw { get; set; }

        public int ConfirmingWithdraw { get; set; }

        public int NewUser24H { get; set; }

        public int NewUser7D { get; set; }

        public int NewUser30D { get; set; }

        #endregion
    }
}
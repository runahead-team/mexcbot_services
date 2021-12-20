using System.Collections.Generic;

namespace multexbot.Api.ResponseModels.Fund
{
    public class BalanceResponse
    {
        public string Asset { get; set; }
        
        public string AssetFullName { get; set; }

        public decimal Amount { get; set; }

        public decimal BlockAmount { get; set; }
        
        public bool WithdrawEnable { get; set; }
        
        public bool DepositEnable { get; set; }
        
        public bool TransferEnable { get; set; }
        
        public decimal TransferFee { get; set; }
        
        public decimal MinimumTransfer { get; set; }
        
        public decimal UsdPrice { get; set; }
        
        public List<NetworkResponse> Networks { get; set; }
        
    }

    public class NetworkResponse
    {
        public string Network { get; set; }
        
        public bool HasAddressTag { get; set; }

        public bool WithdrawEnable { get; set; }

        public decimal WithdrawFee { get; set; }

        public decimal MinimumWithdraw { get; set; }

        public bool DepositEnable { get; set; }
       
    }
}
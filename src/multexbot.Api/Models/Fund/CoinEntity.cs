using System.ComponentModel.DataAnnotations;

namespace multexbot.Api.Models.Fund
{
    public class CoinEntity
    {
        [Required]
        public string Coin { get; set; }

        [Required]
        public string Network { get; set; }

        [Required]
        public string FullName { get; set; }

        public bool HasAddressTag { get; set; }

        public bool WithdrawEnable { get; set; }

        [Required]
        [Range(0,double.MaxValue)]
        public decimal WithdrawFee { get; set; }

        [Required]
        [Range(0,double.MaxValue)]
        public decimal MinimumWithdraw { get; set; }

        public bool DepositEnable { get; set; }

        [Required]
        [Range(0,double.MaxValue)]
        public decimal TransferFee { get; set; }

        [Required]
        [Range(0,double.MaxValue)]
        public decimal MinimumTransfer { get; set; }
        
        public bool TransferEnable { get; set; }

        public bool Enable { get; set; }
        
        public bool? IsDefault { get; set; }

        public bool IsFixedPrice { get; set; }

        [Required]
        [Range(0,double.MaxValue)]
        public decimal UsdPrice { get; set; }

        public long PriceUpdatedTime { get; set; }

        public long CreatedTime { get; set; }
    }
}
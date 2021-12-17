using System.ComponentModel.DataAnnotations;
using multexBot.Api.Constants;

namespace multexBot.Api.RequestModels.Fund
{
    public class WithdrawRequest
    {
        [Required]
        [MaxLength(16)] 
        public string Asset { get; set; }

        [MaxLength(16)] 
        public string Network { get; set; }

        [MaxLength(128)] 
        public string Address { get; set; }

        [MaxLength(128)] 
        public string AddressTag { get; set; }

        [Required] 
        [Range(0, double.MaxValue)] 
        public decimal Amount { get; set; }

        [MaxLength(6)] public string GaCode { get; set; }

    }
}
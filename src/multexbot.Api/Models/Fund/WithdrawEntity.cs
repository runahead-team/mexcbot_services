using multexBot.Api.Constants;
using multexBot.Api.RequestModels.Fund;
using sp.Core.Models;
using sp.Core.Utils;

namespace multexBot.Api.Models.Fund
{
    public class WithdrawEntity
    {
        public WithdrawEntity()
        {
            
        }
        
        public WithdrawEntity(WithdrawRequest request, AppUser appUser)
        {
            Guid = AppUtils.NewGuidStr();
            UserId = appUser.Id;
            Username = appUser.Username;
            Email = appUser.Email;
            Asset = request.Asset;
            Network = request.Network;
            Address = request.Address;
            AddressTag = string.IsNullOrEmpty(request.AddressTag) ? null : request.AddressTag;
            TxId = null;
            Status = WithdrawStatus.PENDING;
            CreatedTime = AppUtils.NowMilis();
        }
        
        public long Id { get; set; }

        public string Guid { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }
        
        public string Email { get; set; }

        public string Asset { get; set; }

        public string Network { get; set; }

        public string Address { get; set; }

        public string AddressTag { get; set; }

        public decimal Amount { get; set; }
        
        public decimal UsdAmount { get; set; }

        public decimal Fee { get; set; }
        
        public string TxId { get; set; }

        public WithdrawStatus Status { get; set; }

        public long CreatedTime { get; set; }
        
        public long UpdatedTime { get; set; }

        public string Receiver { get; set; }

        public long? ReceiverId { get; set; }

    }
}
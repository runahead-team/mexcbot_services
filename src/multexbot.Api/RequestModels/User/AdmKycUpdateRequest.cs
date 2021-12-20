using multexbot.Api.Constants;

namespace multexbot.Api.RequestModels.User
{
    public class AdmKycUpdateRequest
    {
        public long Id { get; set; }
        
        public VerifyStatus Status { get; set; }

        public string Note { get; set; }
    }
}
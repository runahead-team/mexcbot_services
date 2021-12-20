using System.Collections.Generic;

namespace multexbot.Api.Models.User
{
    public class VerificationDto : VerificationEntity
    {

        #region Scan Verification

        public int MatchPoints { get; set; }
        public List<string> MatchingItems { get; set; }

        #endregion
    }
}
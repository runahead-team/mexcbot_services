using System;
using System.Collections.Generic;
using multexBot.Api.Infrastructure;
using sp.Core.Constants;
using sp.Core.Utils;

namespace multexBot.Api.Constants
{
    public static class MultexBotConstants
    {
        public const int TokenExpiryInSeconds = 172800;

        public const int AdmTokenExpiryInSeconds = 14400;

        public const int UpdateUsdPriceInterval = 10;
        
        public const int MaxLevel = 3;
        
        public const string DefaultAdminPassword = "MultexBot@goahead@2030";

    }
}
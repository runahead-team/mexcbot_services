using System;
using System.Collections.Generic;
using multexbot.Api.Infrastructure;
using sp.Core.Constants;
using sp.Core.Utils;

namespace multexbot.Api.Constants
{
    public static class MultexBotConstants
    {
        public const int TokenExpiryInSeconds = 172800;

        public const int AdmTokenExpiryInSeconds = 14400;

        //14400s = 4h
        public const int UpdateUsdPriceInterval = 14400;
        
        public const int MaxLevel = 3;
        
        public const string DefaultAdminPassword = "MultexBot@goahead@2030";

        public const string KrwCoin = "KRW";

        public static string[] StableCoins = new string[] { "USDT", "mUSD" };
    }
}
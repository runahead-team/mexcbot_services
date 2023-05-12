using System;
using System.Collections.Generic;
using mexcbot.Api.Infrastructure;
using sp.Core.Constants;
using sp.Core.Utils;

namespace mexcbot.Api.Constants
{
    public static class MexcBotConstants
    {
        public const int TokenExpiryInSeconds = 172800;

        public const int AdmTokenExpiryInSeconds = 14400;

        //14400s = 4h
        public const int UpdateUsdPriceInterval = 14400;

        //60000 = 1p
        public const int ExpiredOrderTime = 60000;
    }
}
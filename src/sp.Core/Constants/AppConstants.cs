namespace sp.Core.Constants
{
    public class AppConstants
    {
        //Trigger build 1
        
        public static readonly string[] UsdStableCoins = new string[] { "USDT", "USDC", "mUSD", "BUSD" };

        public const string DateFormat = "yyyy-MM-dd";
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        /*
         * username is 8-20 characters long
         * no _ or . at the beginning
         * no _ or . at the end
         * no __ or _. or ._ or .. inside
         */
        public const string UsernameRegex = @"^(?=[a-zA-Z0-9._]{6,32}$)(?!.*[_.]{2})[^_.].*[^_.]$";
        
        public const char Delimiter = ';';

    }

    public class AppEnvironments
    {
        public const string Production = "Production";
        public const string Staging = "Staging";
        public const string Development = "Development";

    }
}
using System.Collections.Generic;

namespace mexcbot.Api.Infrastructure
{
    public class Configurations
    {
        public static string DbConnectionString { get; set; }

        public static string AppName { get; set; }

        public static string RsaPrvKey { get; set; }

        public static string AdmRsaPrvKey { get; set; }

        public static string HashKey { get; set; }

        public static string[] AllowOrigins { get; set; }
        
        public static string MexcUrl { get; set; }
        
        public static string LBankUrl { get; set; }
        
        public static string DeepCoinUrl { get; set; }
        
        public static Dictionary<string, List<object>> Enums { get; set; }
    }
}
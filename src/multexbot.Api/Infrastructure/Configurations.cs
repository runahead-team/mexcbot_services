using System.Collections.Generic;

namespace multexbot.Api.Infrastructure
{
    public class Configurations
    {
        public static string DbConnectionString { get; set; }

        public static string AppName { get; set; }

        public static string RsaPrvKey { get; set; }

        public static string AdmRsaPrvKey { get; set; }

        public static string HashKey { get; set; }

        public static SendGridConfig SendGrid { get; set; }

        public static string TelegramBot { get; set; }

        public static int TelegramGroup { get; set; }

        public static string[] AllowOrigins { get; set; }

        public static Dictionary<string, List<object>> Enums { get; set; }

        public static string ReCaptchaSecretKey { get; set; }
        
        public static string UpbitUrl { get; set; }
        
        public static string FlataUrl { get; set; }
        
        public static string SpExchangeUrl { get; set; }
        
        public static string LBankUrl { get; set; }
        
        public static string BingxUrl { get; set; }
        
        public static OpenExchangeRatesConfig OpenExchangeRates { get; set; }
    }

    public class SendGridConfig
    {
        public string ApiKey { get; set; }

        public string SenderEmail { get; set; }

        public string DisplayName { get; set; }
    }
    
    public class OpenExchangeRatesConfig
    {
        public string AppId { get; set; }
    }
}
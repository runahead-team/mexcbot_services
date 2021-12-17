using System;
using System.Text;
using sp.Core.Constants;

namespace sp.Core.Utils
{
    public class AppUtils
    {
        #region Guid

        public static string NewGuidStr()
        {
            return Guid.NewGuid().ToString("N");
        }

        #endregion

        #region Datetime

        public static long NowMilis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static long NowDateMilis()
        {
            var now = NowMilis();

            return now - now % (long) TimeSpan.FromDays(1).TotalMilliseconds;
        }
        
        public static long YesterdayDateMilis()
        {
            return NowDateMilis() -  (long) TimeSpan.FromHours(24).TotalMilliseconds;
        }

        public static DateTime DateTimeFromMilis(long unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return dtDateTime.AddMilliseconds(unixTimeStamp).ToUniversalTime();
        }

        public static DateTime NowDate()
        {
            return DateTime.UtcNow;
        }

        public static long DateTimeToUnixInMilliSeconds(DateTime dateTime)
        {
            return (long) dateTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        #endregion

        #region String

        public static string RandomString(int length = 10)
        {
            var rand = new Random();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var builder = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var c = pool[rand.Next(0, pool.Length)];
                builder.Append(c);
            }

            return builder.ToString();
        }

        public static string RandomNumber(int length = 10)
        {
            var rand = new Random();
            const string pool = "0123456789";
            var builder = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var c = pool[rand.Next(0, pool.Length)];
                builder.Append(c);
            }

            return builder.ToString();
        }

        #endregion

        #region Environment

        public static string GetEnv()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        }

        #endregion
    }
}
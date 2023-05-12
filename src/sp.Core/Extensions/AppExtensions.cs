using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Utils;

namespace sp.Core.Extensions
{
    public static class AppExtensions
    {
        #region String

        public static string ToSearchString(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = text.ToLower();

            text = text.Replace(" ", "").ToLower();
            text = text.Replace("đ", "d").ToLower();

            var special = new[]
            {
                "`", "~", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_", "=", "+",
                "{", "}", "[", "]", ";", ":", "'", "\"", "<", ",", ">", ".", "?", "/", "\\", "|"
            };

            text = special.Aggregate(text, (current, sp) => current.Replace(sp, ""));

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }
        
        public static string ToSlug(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = text.ToLower();

            text = text.Replace(" ", "-").ToLower();
            text = text.Replace("đ", "d").ToLower();

            var special = new[]
            {
                "`", "~", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_", "=", "+",
                "{", "}", "[", "]", ";", ":", "'", "\"", "<", ",", ">", ".", "?", "/", "\\", "|"
            };

            text = special.Aggregate(text, (current, sp) => current.Replace(sp, ""));

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }
        
        public static void ValidateEmail(this string value)
        {
            try
            {
                var mailAddress = new MailAddress(value);
            }
            catch (Exception)
            {
                throw new AppException("Mail address is invalid");
            }
        }

        public static string ToSha512Hash(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            using (var sha512 = SHA512.Create())
            {
                var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(value));

                var stringBuilder = new StringBuilder();

                foreach (var b in hash)
                    stringBuilder.AppendFormat(b.ToString("x2"));
                return stringBuilder.ToString().ToUpper();
            }
        }

        public static string ToSha1Hash(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            using (var sha512 = SHA1.Create())
            {
                var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(value));

                var stringBuilder = new StringBuilder();

                foreach (var b in hash)
                    stringBuilder.AppendFormat(b.ToString("x2"));
                return stringBuilder.ToString().ToUpper();
            }
        }

        public static string Encrypt(this string plainText, string hashKey)
        {
            byte[] iv = new byte[16];  
            byte[] array;  
  
            using (Aes aes = Aes.Create())  
            {  
                aes.Key = Encoding.UTF8.GetBytes(hashKey);  
                aes.IV = iv;  
  
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);  
  
                using (MemoryStream memoryStream = new MemoryStream())  
                {  
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))  
                    {  
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))  
                        {  
                            streamWriter.Write(plainText);  
                        }  
  
                        array = memoryStream.ToArray();  
                    }  
                }  
            }  
  
            return Convert.ToBase64String(array);  
        }

        public static string Decrypt(this string cipherText, string hashKey)
        {
            byte[] iv = new byte[16];  
            byte[] buffer = Convert.FromBase64String(cipherText);  
  
            using (Aes aes = Aes.Create())  
            {  
                aes.Key = Encoding.UTF8.GetBytes(hashKey);  
                aes.IV = iv;  
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);  
  
                using (MemoryStream memoryStream = new MemoryStream(buffer))  
                {  
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))  
                    {  
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))  
                        {  
                            return streamReader.ReadToEnd();  
                        }  
                    }  
                }  
            }  
        }

        #endregion

        #region INumberable

        public static string ToNumbersString(this IEnumerable<long> numbers)
        {
            return $"[{string.Join(";", numbers)}]";
        }

        public static string ToNumbersString(this long[] numbers)
        {
            return $"[{string.Join(";", numbers)}]";
        }

        #endregion

        #region Decimals

        public static string ToCurrencyString(this decimal value)
        {
            if (value == 0m)
                return "0.00";

            return value.ToString("N8");
        }

        public static string ConcatPrecision(this decimal value)
        {
            try
            {
                var str = value.ToString("F8");

                var precisions = str.Split('.')[1].Reverse();

                var precision = 8;

                foreach (var c in precisions)
                {
                    if (int.Parse(c.ToString()) > 0)
                        break;

                    precision--;
                }

                return value.ToString($"F{precision}");
            }
            catch (Exception)
            {
                return value.ToString("F8");
            }
        }

        public static decimal Truncate(this decimal value, int precision)
        {
            try
            {
                if (precision > 8)
                    precision = 8;

                if (precision == 0)
                    return Math.Truncate(value);

                var scale = (decimal) Math.Pow(10, precision);

                return Math.Truncate(value * scale) / scale;
            }
            catch (Exception)
            {
                return value;
            }
        }

        #endregion

        #region Long

        public static string ToDateTimeStr(this long unixTimestampMilis)
        {
            try
            {
                return unixTimestampMilis <= 0
                    ? "zero_time"
                    : AppUtils.DateTimeFromMilis(unixTimestampMilis).ToString(AppConstants.DateTimeFormat);
            }
            catch (Exception)
            {
                return "error_time";
            }
        }

        public static string ToDateStr(this long unixTimestampMilis)
        {
            try
            {
                return unixTimestampMilis <= 0
                    ? "zero_time"
                    : AppUtils.DateTimeFromMilis(unixTimestampMilis).ToString(AppConstants.DateFormat);
            }
            catch (Exception)
            {
                return "error_time";
            }
        }

        #endregion
    }
}
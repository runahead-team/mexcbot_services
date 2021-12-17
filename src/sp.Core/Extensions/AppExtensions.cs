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

        private const int KeySize = 256;

        public static string Encrypt(this string plainText, string hashKey)
        {
            var initVector = hashKey.Substring(0, 16);

            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            var password = new PasswordDeriveBytes(hashKey, null);
#pragma warning disable 618
            var keyBytes = password.GetBytes(KeySize / 8);
#pragma warning restore 618
            var symmetricKey = new AesManaged
            {
                Padding = PaddingMode.Zeros,
                Mode = CipherMode.CBC
            };
            var encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
            var memoryStream = new MemoryStream();
            var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            var cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }

        public static string Decrypt(this string cipherText, string hashKey)
        {
            var initVector = hashKey.Substring(0, 16);

            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            var initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            var cipherTextBytes = Convert.FromBase64String(cipherText);
            var password = new PasswordDeriveBytes(hashKey, null);
#pragma warning disable 618
            var keyBytes = password.GetBytes(KeySize / 8);
#pragma warning restore 618
            var symmetricKey = new AesManaged
            {
                Padding = PaddingMode.Zeros,
                Mode = CipherMode.CBC
            };
            var decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
            var memoryStream = new MemoryStream(cipherTextBytes);
            var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            var plainTextBytes = new byte[cipherTextBytes.Length];
            var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
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

            if (value > 1)
            {
                return value.ToString("N2");
            }

            if (value > 0.1m)
            {
                return value.ToString("N3");
            }

            if (value > 0.01m)
            {
                return value.ToString("N4");
            }

            if (value > 0.001m)
            {
                return value.ToString("N5");
            }

            if (value > 0.0001m)
            {
                return value.ToString("N6");
            }

            if (value > 0.00001m)
            {
                return value.ToString("N7");
            }

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
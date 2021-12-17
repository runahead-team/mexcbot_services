using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Extensions;
using sp.Core.Token.Models;
using sp.Core.Utils;

namespace sp.Core.Token
{
    public abstract class BaseTokenManager
    {
        private readonly string _hashKey;

        protected BaseTokenManager(string hashKey)
        {
            _hashKey = hashKey;
        }

        #region Token

        public async Task<string> GenToken(TokenType type, long userId, int expMin = 10, string data = null)
        {
            var expTime = AppUtils.NowMilis() + (long) TimeSpan.FromMinutes(expMin).TotalMilliseconds;

            var guid = AppUtils.NewGuidStr();

            var token = new Models.Token
            {
                Guid = guid,
                Type = type,
                UserId = userId,
                Data = data,
                ExpTime = expTime
            };

            var publicToken = guid.Encrypt(_hashKey);
            publicToken = Encode(publicToken);

            var tokenData = JsonConvert.SerializeObject(token)
                .Encrypt(_hashKey);

            await SaveToken(guid, tokenData);

            return publicToken;
        }

        public async Task<Models.Token> PopToken(TokenType type, string publicToken)
        {
            try
            {
                publicToken = Decode(publicToken);

                var guid = publicToken.Decrypt(_hashKey);

                var encryptedData = await GetToken(guid);

                if (string.IsNullOrEmpty(encryptedData))
                    throw new AppException(AppError.TOKEN_WRONG, "Token is already used");

                var data = encryptedData.Decrypt(_hashKey);

                var token = JsonConvert.DeserializeObject<Models.Token>(data);

                if (token == null)
                    throw new AppException(AppError.TOKEN_INVALID, "Token is invalid");

                if (token.Type != type)
                    throw new AppException(AppError.TOKEN_INVALID, "Token is not valid");

                if (token.ExpTime < AppUtils.NowMilis())
                    throw new AppException(AppError.TOKEN_EXPIRED, "Token is expired");

                await DeleteToken(guid);

                return token;
            }
            catch (Exception e)
            {
                if (e is AppException)
                    throw;

                throw new AppException(AppError.TOKEN_INVALID, "Token is invalid");
            }
        }

        #endregion

        #region Disable Token

        public string GenDisableToken(long userId)
        {
            var guid = AppUtils.NewGuidStr();

            var tokenBody = $"{guid};{userId}";
            var publicToken = tokenBody.Encrypt(_hashKey);
            publicToken = Encode(publicToken);

            return publicToken;
        }

        public Models.Token PopDisableToken(string publicToken)
        {
            try
            {
                publicToken = Decode(publicToken);

                var token = publicToken.Decrypt(_hashKey);

                var data = token.Split(";");

                if (data.Length != 2)
                    throw new AppException(AppError.TOKEN_INVALID, "Token is invalid");

                if (!long.TryParse(data[1], out var userId))
                    throw new AppException(AppError.TOKEN_INVALID, "Token is invalid");

                return new Models.Token
                {
                    Guid = data[0],
                    UserId = userId
                };
            }
            catch (Exception e)
            {
                if (e is AppException)
                    throw;

                throw new AppException(AppError.TOKEN_INVALID, "Token is invalid");
            }
        }

        #endregion

        #region Otp

        public async Task<string> GenOtp(TokenType type, long userId, long refId, string key, string data = null,
            int expMin = 10
        )
        {
            var expTime = AppUtils.NowMilis() + (long) TimeSpan.FromMinutes(expMin).TotalMilliseconds;

            var otp = AppUtils.RandomNumber(6);

            var hashInput = userId + refId + key + type.ToString("G") + otp;
            var hash = hashInput.ToSha1Hash();

            var otpObj = new Otp
            {
                Hash = hash,
                Type = type,
                ExpTime = expTime,
                UserId = userId,
                RefId = refId,
                Key = key,
                Data = data
            };

            var optData = JsonConvert.SerializeObject(otpObj)
                .Encrypt(_hashKey);

            await SaveToken(hash, optData);

            return otp;
        }

        public async Task<Otp> GetOtp(TokenType type, long userId, long refId, string key, string otp)
        {
            try
            {
                var hashInput = userId + refId + key + type.ToString("G") + otp;
                var hash = hashInput.ToSha1Hash();

                var encryptedData = await GetToken(hash);

                if (string.IsNullOrEmpty(encryptedData))
                    throw new AppException(AppError.OTP_WRONG, "OTP is wrong or already used");

                var data = encryptedData.Decrypt(_hashKey);

                var otpObj = JsonConvert.DeserializeObject<Otp>(data);

                if (otpObj == null)
                    throw new AppException(AppError.OTP_WRONG, "OTP is wrong or already used");

                if (otpObj.Type != type)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.UserId != userId)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.RefId != refId)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.Key != key)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.ExpTime < AppUtils.NowMilis())
                    throw new AppException(AppError.OTP_EXPIRED, "OTP is expired");

                return otpObj;
            }
            catch (Exception e)
            {
                if (e is AppException)
                    throw;

                throw new AppException(AppError.OTP_WRONG, "OTP is wrong or already used");
            }
        }

        public async Task DelOtp(TokenType type, long userId, long refId, string key, string otp)
        {
            try
            {
                var hashInput = userId + refId + key + type.ToString("G") + otp;
                var hash = hashInput.ToSha1Hash();

                await DeleteToken(hash);
            }
            catch (Exception e)
            {
                if (e is AppException)
                    throw;

                throw new AppException(AppError.OTP_WRONG, "OTP is wrong or already used");
            }
        }

        public async Task<Otp> PopOtp(TokenType type, long userId, long refId, string key, string otp)
        {
            try
            {
                var hashInput = userId + refId + key + type.ToString("G") + otp;
                var hash = hashInput.ToSha1Hash();

                var encryptedData = await GetToken(hash);

                if (string.IsNullOrEmpty(encryptedData))
                    throw new AppException(AppError.OTP_WRONG, "OTP is wrong or already used");

                var data = encryptedData.Decrypt(_hashKey);

                var otpObj = JsonConvert.DeserializeObject<Otp>(data);

                if (otpObj == null)
                    throw new AppException(AppError.OTP_WRONG, "OTP is wrong or already used");

                if (otpObj.Type != type)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.UserId != userId)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.RefId != refId)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.Key != key)
                    throw new AppException(AppError.OTP_INVALID, "OTP is not valid");

                if (otpObj.ExpTime < AppUtils.NowMilis())
                    throw new AppException(AppError.OTP_EXPIRED, "OTP is expired");

                await DeleteToken(hash);

                return otpObj;
            }
            catch (Exception e)
            {
                if (e is AppException)
                    throw;

                throw new AppException(AppError.OTP_WRONG, "OTP is wrong or already used");
            }
        }

        #endregion

        protected abstract Task SaveToken(string id, string data);

        protected abstract Task<string> GetToken(string id);

        protected abstract Task DeleteToken(string id);

        #region Private methods

        private static string Encode(string input)
        {
            var byteArray = Encoding.UTF8.GetBytes(input);

            input = Convert.ToBase64String(byteArray);

            return input.Replace('+', '.').Replace('/', '_').Replace('=', '-');
        }

        private static string Decode(string input)
        {
            input = input.Replace('.', '+').Replace('_', '/').Replace('-', '=');

            var byteArray = Convert.FromBase64String(input);

            return Encoding.UTF8.GetString(byteArray);
        }

        #endregion
    }
}
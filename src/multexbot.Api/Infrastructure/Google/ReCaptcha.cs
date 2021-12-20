using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Utils;

namespace multexbot.Api.Infrastructure.Google
{
    public static class ReCaptcha
    {
        public static async Task Validate(string reCaptcha)
        {
            if (AppUtils.GetEnv() != AppEnvironments.Production)
                return;

            if (string.IsNullOrEmpty(reCaptcha))
                throw new AppException(AppError.INVALID_OPERATION, "Invalid ReCaptcha");
            
            try
            {
                var httpClient = new HttpClient();

                var requestBody = new StringContent(string.Empty);

                var response = await httpClient
                    .PostAsync(
                        $"https://www.google.com/recaptcha/api/siteverify?secret={Configurations.ReCaptchaSecretKey}&response={reCaptcha}",
                        requestBody);

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new AppException(AppError.INVALID_OPERATION, "Invalid ReCaptcha");

                var responseResult = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(responseResult))
                    throw new AppException(AppError.INVALID_OPERATION, "Invalid ReCaptcha");

                var result = JsonConvert.DeserializeObject<ReCaptchaResponse>(responseResult);

                if (!result.Success)
                    throw new AppException(AppError.INVALID_OPERATION, "Invalid ReCaptcha");
            }
            catch (Exception e)
            {
                if (e is AppException)
                    throw;

                Log.Error(e, "ReCaptcha");
            }
        }
    }

    public class ReCaptchaResponse
    {
        [JsonProperty("success")] public bool Success { get; set; }

        [JsonProperty("challenge_ts")] public DateTime Time { get; set; }

        [JsonProperty("hostname")] public string Hostname { get; set; }

        [JsonProperty("error-codes")] public object ErrorCode { get; set; }
    }
}
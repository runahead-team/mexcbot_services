using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mexcbot.Api.Models.User;
using mexcbot.Api.RequestModels.User;
using mexcbot.Api.Services.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Extensions;

namespace mexcbot.Api.Services
{
    public class UserService : IUserService
    {
        public UserService()
        {
        }

        public async Task<UserDto> LoginAsync(LoginRequest request)
        {
            request.Password = request.Password.ToSha512Hash();

            if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.Username))
                throw new AppException(AppError.INVALID_PARAMETERS);

            var users = await LoadUserFromJson();

            var user = users.FirstOrDefault(x => (x.Email == request.Email || x.Username == request.Username));

            if (user == null)
                throw new AppException(AppError.ACCOUNT_NOT_FOUND);

            if (user.Password != request.Password)
                throw new AppException(AppError.PASSWORD_WRONG);

            return user;
        }

        #region Private

        private static async Task<List<UserDto>> LoadUserFromJson()
        {
            using var r = new StreamReader("../mexcbot.Api/JsonData/Users.json");
            var json = await r.ReadToEndAsync();
            
            var data = JObject.Parse(json)["Users"];
            
            return data != null ? JsonConvert.DeserializeObject<List<UserDto>>(data.ToString()) : new List<UserDto>();
        }

        #endregion
    }
}
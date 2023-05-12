using System.Threading.Tasks;
using mexcbot.Api.Models.User;
using mexcbot.Api.RequestModels.User;

namespace mexcbot.Api.Services.Interface
{
    public interface IUserService
    {
        Task<UserDto> LoginAsync(LoginRequest request);
    }
}
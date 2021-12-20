using System.Threading.Tasks;
using multexbot.Api.Models.User;
using multexbot.Api.RequestModels.User;
using multexbot.Api.ResponseModels.User;
using sp.Core.Models;

namespace multexbot.Api.Services.Interface
{
    public interface IUserService
    {
        Task<PagingResult<UserResponse>> GetAll(TableRequest request);
        
        Task<UserResponse> Get(string account);
        
        Task<AppUser> Register(RegisterRequest request);

        Task SendRegisterOtp(SendRegisterOtpRequest request);

        Task<UserDto> Login(LoginRequest request);

        Task ForgotPassword(string username);

        Task ResetPassword(ResetPasswordRequest request);

        Task ChangePassword(ChangePasswordRequest request, AppUser appUser);

        Task<GaSetupResponse> GetGaSetup(AppUser appUser);

        Task SetupGa(GaSetupRequest request, AppUser appUser);

        Task<MyProfileResponse> GetProfile(long userId);

        Task<UserResponse> UpdateProfile(UserUpdateRequest request, AppUser appUser);
        
        #region Admin

        Task<PagingResult<UserDto>> AdmGetList(TableRequest request);

        Task AdmDisable(long userId, AppUser admin);

        Task AdmActive(long userId, AppUser admin);

        Task AdmDisableGa(long userId, AppUser admin);

        #endregion

        #region System

        Task<UserDto> SysGetUser(long userId);
        
        Task<UserDto> SysGetUser(string username);
        
        Task SysCheckGa(long userId, string code, bool mustEnable = false);
        
        #endregion
    }
}
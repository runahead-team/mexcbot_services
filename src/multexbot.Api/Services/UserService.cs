using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GoogleAuthenticatorService.Core;
using multexbot.Api.Constants;
using multexbot.Api.Infrastructure;
using multexbot.Api.Models.User;
using multexbot.Api.RequestModels.User;
using multexbot.Api.ResponseModels.User;
using multexbot.Api.Services.Interface;
using multexbot.Api.Services.SubService;
using MySqlConnector;
using Serilog;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Extensions;
using sp.Core.Models;
using sp.Core.Utils;

namespace multexbot.Api.Services
{
    public class UserService : IUserService
    {
        private readonly TokenManager _tokenManager;
        private readonly Mailer _mailer;

        public UserService(
            TokenManager tokenManager,
            Mailer mailer)
        {
            _tokenManager = tokenManager;
            _mailer = mailer;
        }

        public async Task<PagingResult<UserResponse>> GetAll(TableRequest request)
        {
            
            var builder = new SqlBuilder();

            #region Filters

            if (request.Filters.TryGetValue("id", out var idFilter))
            {
                if (long.TryParse(idFilter, out var id) && id > 0)
                    builder.Where("Id = @Id", new {Id = id});
            }

            if (request.Filters.TryGetValue("username", out var username) && !string.IsNullOrEmpty(username))
            {
                builder.Where("Username LIKE @Username", new {Username = $"%{username.ToLower()}%"});
            }

            if (request.Filters.TryGetValue("account", out var account) && !string.IsNullOrEmpty(account))
            {
                builder.Where("Account LIKE @Account", new {Account = account});
            }

            if (request.Filters.TryGetValue("sponsorId", out var sponsorIdFilter))
            {
                if (long.TryParse(sponsorIdFilter, out var sponsorId) && sponsorId > 0)
                    builder.Where("SponsorId = @SponsorId", new {SponsorId = sponsorId});
            }

            #endregion

            var (skip, take) = request.GetSkipTake();

            var counter = builder.AddTemplate("SELECT COUNT(1) FROM Users /**where**/");

            var orderBy = request.GetOrderByText(typeof(UserResponse));

            var selector =
                builder.AddTemplate(
                    $"SELECT * FROM Users /**where**/ {orderBy} LIMIT @Skip, @Take",
                    new {Skip = skip, Take = take});

            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);
            await sqlConnection.OpenAsync();

            var total = await sqlConnection.ExecuteScalarAsync<int>(
                counter.RawSql, counter.Parameters);

            var items = (await sqlConnection.QueryAsync<UserResponse>(
                selector.RawSql, selector.Parameters)).ToList();

            return new PagingResult<UserResponse>(items, total, request);
        }

        public async Task<UserResponse> Get(string account)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserResponse>(
                @"SELECT * FROM Users WHERE `Account` = @Account LIMIT 1",
                new
                {
                    Account = account
                });

            return user;
        }

        public async Task<AppUser> Register(RegisterRequest request)
        {
            //Format input
            request.Username = request.Username.ToLower();
            request.Email = request.Email.ToLower();
            request.Email.ValidateEmail();
            request.Password = request.Password.ToSha512Hash();

            if (!request.Email.EndsWith("stepwatch.io")
                && !request.Email.EndsWith("splabs.info"))
                throw new AppException("Email is not allow");
            
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            #region Validation

            //TRICK Production only
            if (AppUtils.GetEnv() == AppEnvironments.Production)
            {
                if (await dbConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM Users WHERE Username = @Username OR Email = @Email LIMIT 1", new
                    {
                        Username = request.Username,
                        Email = request.Email
                    }) != 0)
                    throw new AppException(AppError.ACCOUNT_EXIST, "Your register username/email already taken");
            }
            else
            {
                if (await dbConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM Users WHERE Username = @Username LIMIT 1", new
                    {
                        Username = request.Username,
                        Email = request.Email
                    }) != 0)
                    throw new AppException(AppError.ACCOUNT_EXIST, "Your register username already taken");
            }

            //TRICK Production only
            if (AppUtils.GetEnv() == AppEnvironments.Production)
            {
                var otp = await _tokenManager.PopOtp(TokenType.REGISTER_OTP, 0, 0, request.Username,
                    request.EmailOtp);
                if (otp == null || otp.Key != request.Username || otp.Data != request.Email)
                    throw new AppException(AppError.OTP_INVALID);
            }
           
            #endregion

            var sponsor =
                await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                    "SELECT Id, Username FROM Users WHERE Id = @Id OR Username = @Username LIMIT 1",
                    new {Id = request.SponsorId, Username = request.SponsorUsername});

            #region Insert User

            var user = new UserEntity(request);
            if (sponsor != null)
                user.SponsorId = sponsor.Id;

            var exec = await dbConnection.ExecuteAsync(
                @"INSERT INTO Users(`Username`,`Email`,`Password`,`SponsorId`,`CreatedTime`)
                VALUES(@Username,@Email,@Password,@SponsorId,@CreatedTime)",
                user);

            if (exec == 0)
            {
                user.Password = string.Empty;
                Log.Error("Insert Users {@data}", user);
                throw new AppException("Register fail");
            }

            user = await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                "SELECT * FROM Users WHERE Username = @Username",
                new
                {
                    Username = request.Username
                });

            if (user == null)
                throw new AppException("Register fail");

            #endregion

            await _mailer.SendWelcome(user.Email, user.Username);
            
            return new AppUser
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Scopes = new List<string>()
            };
        }

        public async Task SendRegisterOtp(SendRegisterOtpRequest request)
        {
            //Format input
            request.Username = request.Username.ToLower();
            request.Email = request.Email.ToLower();
            request.Email.ValidateEmail();

            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            if (AppUtils.GetEnv() == AppEnvironments.Production)
            {
                if (await dbConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM Users WHERE Username = @Username OR Email = @Email LIMIT 1", new
                    {
                        Username = request.Username,
                        Email = request.Email
                    }) != 0)
                    throw new AppException(AppError.ACCOUNT_EXIST, "Your register username/email already taken");
            }
            else
            {
                if (await dbConnection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM Users WHERE Username = @Username LIMIT 1", new
                    {
                        Username = request.Username,
                        Email = request.Email
                    }) != 0)
                    throw new AppException(AppError.ACCOUNT_EXIST, "Your register username already taken");
            }

            var otpCode = await _tokenManager.GenOtp(TokenType.REGISTER_OTP, 0, 0, request.Username, request.Email);

            await _mailer.SendRegisterOtp(request.Email, request.Username, otpCode);
        }

        public async Task<UserDto> Login(LoginRequest request)
        {
            //Format input
            request.Username = request.Username.ToLower();
            request.Password = request.Password.ToSha512Hash();

            UserEntity user;
            UserDto userDto;

            await using (var dbConnection = new MySqlConnection(Configurations.DbConnectionString))
            {
                user = await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                    "SELECT * FROM Users WHERE Username = @Username", new
                    {
                        Username = request.Username
                    });

                if (user == null)
                    throw new AppException(AppError.PASSWORD_WRONG);

                userDto = await dbConnection.QueryFirstOrDefaultAsync<UserDto>(
                    @"SELECT * FROM Users WHERE Id = @Id LIMIT 1",
                    new
                    {
                        Id = user.Id
                    });
            }

            if (user.Password != request.Password)
                throw new AppException(AppError.PASSWORD_WRONG);

            if (user.Status == UserStatus.BLOCK)
                throw new AppException(AppError.ACCOUNT_BLOCKED);

            if (user.Status == UserStatus.EXPIRED)
                throw new AppException(AppError.ACCOUNT_EXPIRED);

            if (user.Status != UserStatus.ACTIVE)
                throw new AppException("User status undefined");

            if (user.GaEnable)
            {
                if (string.IsNullOrEmpty(request.GaCode))
                    throw new AppException(AppError.GACODE_REQUIRED);

                if (!ValidateGa(user.GaSecret, request.GaCode))
                    throw new AppException(AppError.GACODE_WRONG);
            }

            await _mailer.SendLoginAlert(user.Email, user.Username);

            return userDto;
        }

        public async Task ForgotPassword(string username)
        {
            username = username.ToLower();

            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                "SELECT * FROM Users where Username = @Username", new
                {
                    Username = username
                });

            if (user == null)
                throw new AppException(AppError.ACCOUNT_NOT_FOUND);

            var otpCode =
                await _tokenManager.GenOtp(TokenType.RESET_PASSWORD, user.Id, user.Id, user.Username, user.Email);

            await _mailer.SendResetPasswordOtp(user.Email, user.Username, otpCode);

            Log.Information("UserForgotPassword UserId={0}", user.Id);
        }

        public async Task ResetPassword(ResetPasswordRequest request)
        {
            request.Username = request.Username.ToLower();
            request.Password = request.Password.ToSha512Hash();

            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                "SELECT * FROM Users where Username = @Username", new
                {
                    Username = request.Username
                });

            if (user == null)
                throw new AppException(AppError.OTP_WRONG);

            var otp = await _tokenManager.PopOtp(TokenType.RESET_PASSWORD, user.Id, user.Id, user.Username,
                request.EmailOtp);

            if (otp == null || otp.Key != request.Username || otp.Data != user.Email)
                throw new AppException(AppError.OTP_INVALID);

            var exec = await dbConnection.ExecuteAsync(
                "UPDATE Users SET Password = @Password, PasswordUpdatedTime = @PasswordUpdatedTime WHERE Id = @Id", new
                {
                    Id = user.Id,
                    Password = request.Password,
                    PasswordUpdatedTime = AppUtils.NowMilis()
                });

            if (exec != 1)
            {
                Log.Error("Update Users (Password)", new
                {
                    user.Id
                });

                throw new AppException(AppError.UNKNOWN, "Update password");
            }

            await _mailer.SendChangePwdAlert(user.Email, user.Username);
        }

        public async Task ChangePassword(ChangePasswordRequest request, AppUser appUser)
        {
            request.Password = request.Password.ToSha512Hash();
            request.OldPassword = request.OldPassword.ToSha512Hash();

            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                "SELECT * FROM Users where Username = @Username", new
                {
                    Username = appUser.Username
                });

            if (user == null)
                throw new AppException(AppError.ACCOUNT_NOT_FOUND);

            if (user.Password != request.OldPassword)
                throw new AppException(AppError.PASSWORD_WRONG);

            var exec = await dbConnection.ExecuteAsync(
                "UPDATE Users SET Password = @Password, PasswordUpdatedTime = @PasswordUpdatedTime WHERE Id = @Id", new
                {
                    Id = appUser.Id,
                    Password = request.Password,
                    PasswordUpdatedTime = AppUtils.NowMilis()
                });

            if (exec != 1)
            {
                Log.Error("Update Users (Password)", new
                {
                    appUser.Id
                });

                throw new AppException(AppError.UNKNOWN, "Update password");
            }

            await _mailer.SendChangePwdAlert(appUser.Email, appUser.Username);
        }

        public async Task<GaSetupResponse> GetGaSetup(AppUser appUser)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            if (await dbConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Users WHERE Id = @Id AND GaEnable = @GaEnable", new
                {
                    Id = appUser.Id,
                    GaEnable = true
                }) > 0)
                throw new AppException("Google Authentication is already enabled");

            var gaSecret = AppUtils.RandomString(12);
            var encryptGaSecret = gaSecret.Encrypt(Configurations.HashKey);

            var exec = await dbConnection.ExecuteAsync("UPDATE Users SET GaSecret = @GaSecret WHERE Id = @Id",
                new
                {
                    GaSecret = encryptGaSecret,
                    Id = appUser.Id,
                });

            if (exec != 1)
            {
                Log.Error("Update Users (GaSecret)", new
                {
                    appUser.Id
                });
                throw new AppException(AppError.UNKNOWN, "Update GaSecret");
            }

            var twoFacAuth = new TwoFactorAuthenticator();
            var setupCode = twoFacAuth
                .GenerateSetupCode(Configurations.AppName, appUser.Username,
                    gaSecret, 500,
                    500);

            return new GaSetupResponse
            {
                QrCodeSetupImageUrl = setupCode.QrCodeSetupImageUrl,
                ManualEntryKey = setupCode.ManualEntryKey
            };
        }

        public async Task SetupGa(GaSetupRequest request, AppUser appUser)
        {
            request.Password = request.Password.ToSha512Hash();

            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                "SELECT Id, GaSecret, GaEnable, Password FROM Users WHERE Id = @Id AND GaEnable = @GaEnable",
                new
                {
                    Id = appUser.Id,
                    GaEnable = !request.Enable
                });

            if (user == null)
                throw new AppException($"Google Authentication is already {(request.Enable ? "enabled" : "disabled")}");

            if (user.Password != request.Password)
                throw new AppException(AppError.PASSWORD_WRONG);

            if (request.Enable)
            {
                var twoFacAuth = new TwoFactorAuthenticator();
                var key = user.GaSecret.Decrypt(Configurations.HashKey);

                if (!twoFacAuth.ValidateTwoFactorPIN(key, request.GaCode))
                    throw new AppException(AppError.GACODE_WRONG);

                var exec = await dbConnection.ExecuteAsync(
                    "UPDATE Users SET GaEnable = @GaEnable WHERE Id = @Id",
                    new
                    {
                        GaEnable = true,
                        Id = user.Id,
                    });

                if (exec != 1)
                {
                    Log.Error("Update Users (GaEnable)", new
                    {
                        appUser.Id
                    });
                    throw new AppException(AppError.UNKNOWN, "Enable Google 2FA");
                }

                Log.Information("UserEnableGa UserId={0}", appUser.Id);
            }
            else if (request.Enable == false)
            {
                var twoFacAuth = new TwoFactorAuthenticator();
                var key = user.GaSecret.Decrypt(Configurations.HashKey);

                if (!twoFacAuth.ValidateTwoFactorPIN(key, request.GaCode))
                    throw new AppException(AppError.GACODE_WRONG);

                var exec = await dbConnection.ExecuteAsync(
                    "UPDATE Users SET GaEnable = @GaEnable WHERE Id = @Id",
                    new
                    {
                        GaEnable = false,
                        Id = user.Id,
                    });

                if (exec != 1)
                {
                    Log.Error("Update Users (GaEnable)", new
                    {
                        appUser.Id
                    });
                    throw new AppException(AppError.UNKNOWN, "Disable Google 2FA");
                }

                Log.Information("UserDisableGa UserId={0}", appUser.Id);
            }
        }

        public async Task<MyProfileResponse> GetProfile(long userId)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var user = await dbConnection.QueryFirstOrDefaultAsync<MyProfileResponse>(
                @"SELECT * FROM Users WHERE Id = @Id",
                new
                {
                    Id = userId
                });

            return user;
        }

        public async Task<UserResponse> UpdateProfile(UserUpdateRequest request, AppUser appUser)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            await dbConnection.ExecuteAsync(
                @"UPDATE Users SET AvatarImage = @AvatarImage, CoverImage = @CoverImage WHERE Id = @Id",
                new
                {
                    Id = appUser.Id,
                    AvatarImage = request.AvatarImage,
                    CoverImage = request.CoverImage
                });

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserResponse>(
                @"SELECT * FROM Users WHERE Id = @Id",
                new
                {
                    Id = appUser.Id
                });

            return user;
        }

        #region Admin

        public async Task<PagingResult<UserDto>> AdmGetList(TableRequest request)
        {
            var builder = new SqlBuilder();

            #region Filters

            if (request.Filters.TryGetValue("id", out var idFilter))
            {
                if (long.TryParse(idFilter, out var id) && id > 0)
                    builder.Where("Id = @Id", new {Id = id});
            }

            if (request.Filters.TryGetValue("userId", out var userFilter))
            {
                if (long.TryParse(userFilter, out var userId) && userId > 0)
                    builder.Where("Id = @UserId", new {UserId = userId});
            }

            if (request.Filters.TryGetValue("username", out var username) && !string.IsNullOrEmpty(username))
            {
                builder.Where("Username LIKE @Username", new {Username = $"%{username.ToLower()}%"});
            }

            if (request.Filters.TryGetValue("sponsorId", out var sponsorIdFilter))
            {
                if (long.TryParse(sponsorIdFilter, out var sponsorId) && sponsorId > 0)
                    builder.Where("SponsorId = @SponsorId", new {SponsorId = sponsorId});
            }

            if (request.Filters.TryGetValue("sponsorUsername", out var sponsorUsername) &&
                !string.IsNullOrEmpty(sponsorUsername))
            {
                builder.Where("SponsorUsername LIKE @SponsorUsername",
                    new {SponsorUsername = $"%{sponsorUsername.ToLower()}%"});
            }

            if (request.Filters.TryGetValue("email", out var email) && !string.IsNullOrEmpty(email))
            {
                builder.Where("Email LIKE @Email", new {Email = $"%{email.ToLower()}%"});
            }

            if (request.Filters.TryGetValue("status", out var statusFilter))
            {
                if (Enum.TryParse(statusFilter, out UserStatus status))
                {
                    builder.Where("Status = @Status", new {Status = status});
                }
            }

            if (request.Filters.TryGetValue("verifyLevel", out var verifyLevelFilter))
            {
                if (Enum.TryParse(verifyLevelFilter, out VerifyLevel verifyLevel))
                {
                    builder.Where("VerifyLevel = @VerifyLevel", new {VerifyLevel = verifyLevel});
                }
            }

            #endregion

            var (skip, take) = request.GetSkipTake();

            var counter = builder.AddTemplate("SELECT COUNT(1) FROM Users /**where**/");

            var selector =
                builder.AddTemplate(
                    "SELECT * FROM Users /**where**/ ORDER BY Id DESC LIMIT @Skip, @Take",
                    new {Skip = skip, Take = take});

            await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);
            await sqlConnection.OpenAsync();

            var total = await sqlConnection.ExecuteScalarAsync<int>(
                counter.RawSql, counter.Parameters);

            var items = (await sqlConnection.QueryAsync<UserDto>(
                selector.RawSql, selector.Parameters)).ToList();

            return new PagingResult<UserDto>(items, total, request);
        }

        public async Task AdmDisable(long userId, AppUser admin)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            if (await dbConnection.ExecuteScalarAsync<int>(@"SELECT COUNT(1) FROM Users WHERE Id = @Id LIMIT 1",
                new
                {
                    Id = userId
                }) != 1)
                throw new AppException(AppError.ACCOUNT_NOT_FOUND);

            var exec = await dbConnection.ExecuteAsync(
                @"UPDATE Users
                SET Status = @Status
                WHERE Id = @Id", new
                {
                    Id = userId,
                    Status = UserStatus.BLOCK
                });

            if (exec != 1)
            {
                Log.Error("DISABLE_USER {@data}", new
                {
                    userId = userId,
                    Status = UserStatus.BLOCK
                });
                throw new AppException(AppError.UNKNOWN);
            }
        }

        public async Task AdmActive(long userId, AppUser admin)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            if (await dbConnection.ExecuteScalarAsync<int>(@"SELECT COUNT(1) FROM Users WHERE Id = @Id LIMIT 1",
                new
                {
                    Id = userId
                }) != 1)
                throw new AppException(AppError.ACCOUNT_NOT_FOUND);

            var exec = await dbConnection.ExecuteAsync(
                @"UPDATE Users
                SET Status = @Status
                WHERE Id = @Id", new
                {
                    Id = userId,
                    Status = UserStatus.ACTIVE
                });

            if (exec != 1)
            {
                Log.Error("ACTIVE_USER {@data}", new
                {
                    userId = userId,
                    Status = UserStatus.ACTIVE
                });
                throw new AppException(AppError.UNKNOWN);
            }
        }

        public async Task AdmDisableGa(long userId, AppUser admin)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            if (await dbConnection.ExecuteScalarAsync<int>(@"SELECT COUNT(1) FROM Users WHERE Id = @Id LIMIT 1",
                new
                {
                    Id = userId
                }) != 1)
                throw new AppException(AppError.ACCOUNT_NOT_FOUND);

            var exec = await dbConnection.ExecuteAsync(
                @"UPDATE Users
                SET GaEnable = @GaEnable
                WHERE Id = @Id", new
                {
                    Id = userId,
                    GaEnable = false,
                });

            if (exec != 1)
            {
                Log.Error("DISABLE_USER_GA {@data}", new
                {
                    userId = userId,
                    GaEnable = false,
                });
                throw new AppException(AppError.UNKNOWN);
            }
        }

        #endregion

        #region System

        public async Task<UserDto> SysGetUser(long userId)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserDto>(
                @"SELECT * FROM Users WHERE Id = @Id",
                new
                {
                    Id = userId
                });

            return user;
        }

        public async Task<UserDto> SysGetUser(string username)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserDto>(
                @"SELECT * FROM Users WHERE Username = @Username LIMIT 1",
                new
                {
                    Username = username
                });

            return user;
        }

        public async Task SysCheckGa(long userId, string code, bool mustEnable = false)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);

            var user = await dbConnection.QueryFirstOrDefaultAsync<UserEntity>(
                "SELECT Id,GaEnable,GaSecret FROM Users WHERE Id = @Id", new
                {
                    Id = userId
                });

            if (user == null)
                throw new AppException(AppError.ACCOUNT_NOT_FOUND);

            if (mustEnable && !user.GaEnable)
                throw new AppException(AppError.GACODE_REQUIRED);

            if (user.GaEnable)
            {
                if (string.IsNullOrEmpty(code))
                    throw new AppException(AppError.GACODE_REQUIRED);

                if (!ValidateGa(user.GaSecret, code))
                    throw new AppException(AppError.GACODE_WRONG);
            }
        }

        #endregion

        #region Private methods

        private bool ValidateGa(string encryptSecret, string gaCode)
        {
            var twoFacAuth = new TwoFactorAuthenticator();
            var key = encryptSecret.Decrypt(Configurations.HashKey);

            return twoFacAuth.ValidateTwoFactorPIN(key, gaCode);
        }

        #endregion
    }
}
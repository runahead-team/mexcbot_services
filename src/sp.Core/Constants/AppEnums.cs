namespace sp.Core.Constants
{
    public enum AppError
    {
        UNKNOWN,
        INVALID_PARAMETERS,
        INVALID_OPERATION,
        TOKEN_INVALID,
        TOKEN_WRONG,
        TOKEN_EXPIRED,
        OTP_INVALID,
        OTP_WRONG,
        OTP_EXPIRED,
        GACODE_REQUIRED,
        PASSWORD_WRONG,
        GACODE_WRONG,
        ACCOUNT_EXIST,
        ACCOUNT_BLOCKED,
        ACCOUNT_EXPIRED,
        ACCOUNT_NOT_FOUND,
        INSUFFICIENT_FUNDS,
        CURRENCY_NOT_SUPPORT,
        WITHDRAW_DISABLE,
        WITHDRAW_BELOW_MINIMUM,
        INVALID_ADDRESS,
        WRONG_CREDENTIALS,
        PERMISSION_DENIED
    }

    public enum MailType
    {
        WELCOME,
        REGISTER_OTP,
        RESET_PASSWORD_OTP,
        CHANGE_PWD_ALERT,
        RECEIVE_DEPOSIT,
        WITHDRAW_ALERT,
        LOGIN_ALERT,
        KYC_REJECT,
        KYC_APPROVE
    }

    public enum TokenType
    {
        REGISTER_OTP,
        LOGIN_OTP,
        RESET_PASSWORD,
        CONFIRM_WITHDRAW,
        WITHDRAW_OTP
    }
}
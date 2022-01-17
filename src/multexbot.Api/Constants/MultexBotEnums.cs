namespace multexbot.Api.Constants
{
    #region User

    public enum UserStatus
    {
        INACTIVE = 0,
        ACTIVE = 1,
        BLOCK = 2,
        EXPIRED = 3
    }

    public enum DocumentType
    {
        ID = 0,
        PASSPORT = 1,
        DRIVER_LICENSE = 2
    }

    public enum VerifyLevel
    {
        NOT_VERIFY = 0,
        PENDING = 1,
        VERIFIED = 2,
        REJECTED = 3
    }

    public enum VerifyStatus
    {   
        NOT_VERIFY = 0,
        PENDING = 1,
        VERIFIED = 2,
        REJECTED = 3,
    }

    public enum UserRank
    {
        BEGINNER = 0,
        TALENTED = 1,
        ADVANCED = 2,
        EXPERT = 3,
        LEGENDARY = 4,
        ALMIGHTY = 5
    }
    #endregion

    #region Fund

    public enum TransactionType
    {
        DEPOSIT = 0,
        WITHDRAW = 1,
        WITHDRAW_CANCEL = 2,
        AIRDROP = 3,
        SELL_NFT = 4,
        BUY_NFT = 5
    }

    public enum WithdrawStatus
    {
        PENDING,
        APPROVED,
        CONFIRMING,
        CONFIRMED,
        CANCELED
    }

    #endregion

    #region ApiKey

    public enum ExchangeType
    {
        SPEXCHANGE = 0,
        UPBIT = 1,
        FLATA = 2,
        LBANK = 3
    }

    #endregion

    #region Order

    public enum OrderStatus
    {
        OPEN,
        PARTIAL_FILLED,
        FILLED,
        CANCELED
    }

    public enum OrderSide
    {
        BUY,
        SELL,
        BOTH
    }

    #endregion
}
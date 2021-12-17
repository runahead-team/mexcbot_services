namespace sp.Core.Constants
{
    public class LogTemplates
    {
        public const string FUND_CHANGE = "FUND_CHANGE UserId={userId} & Asset={asset} & Amount={amount} & BlockAmount={blockAmount} & Amount+={amountChange} & BlockAmount+={blockAmountChange} & Type={type} & Log={log}";

        public const string FUND_CHANGE_ERROR = "FUND_CHANGE_ERROR UserId={userId} & Asset={asset} & Amount={amount} & BlockAmount={blockAmount} & Amount+={amountChange} & BlockAmount+={blockAmountChange} & Type={type} & Log={log}";
        

    }
}

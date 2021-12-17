using sp.Core.Constants;

namespace sp.Core.Token.Models
{
    public class Token
    {
        public string Guid { get; set; }

        public TokenType Type { get; set; }

        public long UserId { get; set; }

        public string Data { get; set; }

        public long ExpTime { get; set; }
    }
   
}
using sp.Core.Constants;

namespace sp.Core.Token.Models
{
    public class TokenBody
    {
        public string Guid { get; set; }

        public TokenType Type { get; set; }

        public long UserId { get; set; }
    }
}
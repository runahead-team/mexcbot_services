using sp.Core.Constants;

namespace sp.Core.Token.Models
{
    public class Otp
    {
        public string Hash { get; set; }
        
        public TokenType Type { get; set; }

        public long UserId { get; set; }
        
        public long RefId { get; set; }

        public string Key { get; set; }
        
        public string Data { get; set; }
        
        public long ExpTime { get; set; }
    }
}
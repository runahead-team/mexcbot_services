namespace multexBot.Api.Infrastructure.Authentication
{
    public class JwtResponse
    {
        public string AccessToken { get; set; }
        
        public long ExpInSeconds { get; set; }
        
    }
}
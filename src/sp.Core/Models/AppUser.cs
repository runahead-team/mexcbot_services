
using System.Collections.Generic;

namespace sp.Core.Models
{
    public class AppUser
    {
        public long Id { get; set; }

        public string Username { get; set; }
        
        public string Email { get; set; }

        public List<string> Scopes { get; set; }
    }
}

using sp.Core.Constants;

namespace sp.Core.Mailer.Models
{
    public class Mail
    {
        public MailType Type { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }
    }
}
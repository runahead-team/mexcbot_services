namespace sp.Core.Mailer.Models
{
    public class MailQueueItem
    {
        public string Subject { get; set; }

        public string Content { get; set; }

        public string Receiver { get; set; }
    }
}
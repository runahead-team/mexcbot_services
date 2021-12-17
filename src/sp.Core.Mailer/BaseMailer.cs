using System.Collections.Generic;
using System.Threading.Tasks;
using sp.Core.Constants;
using sp.Core.Mailer.Models;

namespace sp.Core.Mailer
{
    public abstract class BaseMailer
    {
        protected abstract void SendMail(string subject, string body, string email);
        
        protected abstract void SendMail(string subject, string body, IEnumerable<string> emails);

        protected abstract Task<Mail> GetMailTemplate(MailType type);
    }
}
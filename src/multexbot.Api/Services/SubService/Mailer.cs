using System;
using System.Net;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using sp.Core.Constants;
using sp.Core.Mailer;
using sp.Core.Mailer.Models;
using sp.Core.Utils;
using multexbot.Api.Infrastructure;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using multexbot.Api.Models.Fund;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using sp.Core.Extensions;

namespace multexbot.Api.Services.SubService
{
    public class Mailer : BaseMailer
    {
        private readonly Dictionary<string, string> _mails = new Dictionary<string, string>();

        private Dictionary<string, string> _mailSubjects = new Dictionary<string, string>();

        public Mailer()
        {
            LoadMails();
        }

        protected void SendMail(string subject, string content, params string[] emails)
        {
            var emailsAsLog = string.Join(";", emails);

            Task.Run(async () =>
            {
                try
                {
                    var client = new RestClient(new Uri(Configurations.Mailgun.BaseUrl))
                    {
                        Authenticator = new HttpBasicAuthenticator("api", Configurations.Mailgun.ApiKey)
                    };

                    var request = new RestRequest();
                    request.AddParameter("domain", Configurations.Mailgun.Domain, ParameterType.UrlSegment);
                    request.Resource = "{domain}/messages";
                    request.AddParameter("from", Configurations.Mailgun.Sender);
                    foreach (var email in emails)
                    {
                        request.AddParameter("to", email);
                    }

                    request.AddParameter("subject", subject);
                    request.AddParameter("html", content);
                    request.Method = Method.Post;

                    var response = await client.ExecuteAsync(request);

                    if (response.StatusCode != HttpStatusCode.OK)
                        Log.Error("Mailer sent {0} {1} {2}", emailsAsLog, subject, response.StatusCode);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Mailer send {0} {1}", emailsAsLog, subject);
                }
            });
        }


        protected override void SendMail(string subject, string body, string email)
        {
            SendMail(subject, body, email);
        }

        protected override void SendMail(string subject, string body, IEnumerable<string> emails)
        {
            SendMail(subject, body, emails);
        }

        protected override Task<Mail> GetMailTemplate(MailType type)
        {
            var mail = new Mail
            {
                Content = $"Content_{type:G}",
                Subject = $"Subject_{type:G}",
            };

            if (_mails.TryGetValue(Enum.GetName(typeof(MailType), type) ?? string.Empty, out var content) &&
                !string.IsNullOrEmpty(content))
                mail.Content = content;

            if (_mailSubjects.TryGetValue(Enum.GetName(typeof(MailType), type) ?? string.Empty, out var subject) &&
                !string.IsNullOrEmpty(subject))
                mail.Subject = subject;

            return Task.FromResult(mail);
        }

        public async Task SendRegisterOtp(string email, string username, string otp)
        {
            var mail = await GetMailTemplate(MailType.REGISTER_OTP);

            mail.Content = mail.Content.Replace("{{username}}", username);
            mail.Content = mail.Content.Replace("{{otp}}", otp);

            SendMail(mail.Subject, mail.Content, email);
        }

        public async Task SendWelcome(string email, string username)
        {
            var mail = await GetMailTemplate(MailType.WELCOME);

            mail.Content = mail.Content.Replace("{{username}}", username);

            SendMail(mail.Subject, mail.Content, email);
        }


        public async Task SendLoginAlert(string email, string username)
        {
            var mail = await GetMailTemplate(MailType.LOGIN_ALERT);

            mail.Content = mail.Content.Replace("{{username}}", username);
            mail.Content = mail.Content.Replace("{{time}}", AppUtils.NowDate().ToString("g"));

            SendMail(mail.Subject, mail.Content, email);
        }

        public async Task SendResetPasswordOtp(string email, string username, string otp)
        {
            var mail = await GetMailTemplate(MailType.RESET_PASSWORD_OTP);

            mail.Content = mail.Content.Replace("{{username}}", username);
            mail.Content = mail.Content.Replace("{{otp}}", otp);

            SendMail(mail.Subject, mail.Content, email);
        }

        public async Task SendChangePwdAlert(string email, string username)
        {
            var mail = await GetMailTemplate(MailType.CHANGE_PWD_ALERT);

            mail.Content = mail.Content.Replace("{{username}}", username);
            mail.Content = mail.Content.Replace("{{time}}", AppUtils.NowDate().ToString("g"));

            SendMail(mail.Subject, mail.Content, email);
        }

        private void LoadMails()
        {
            #region Mails

            var dic = new DriveInfo(@"./");

            var dirInfo = dic.RootDirectory;

            var fileNames = dirInfo.GetFiles("*.html");

            foreach (var file in fileNames)
            {
                var content = File.ReadAllText(file.FullName);

                _mails.Add(file.Name.Split('.')[0], content);
            }

            #endregion

            #region Subjects

            dic = new DriveInfo(@"./");

            dirInfo = dic.RootDirectory;

            fileNames = dirInfo.GetFiles("mail_subjects.json");

            foreach (var file in fileNames)
            {
                var subjects = File.ReadAllText(file.FullName);

                _mailSubjects = JsonConvert.DeserializeObject<Dictionary<string, string>>(subjects);
            }

            #endregion
        }
    }
}
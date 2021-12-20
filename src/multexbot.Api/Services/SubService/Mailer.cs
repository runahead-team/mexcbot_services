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

        protected override void SendMail(string subject, string content, string email)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = new SendGridClient(Configurations.SendGrid.ApiKey);
                    var from = new EmailAddress(Configurations.SendGrid.SenderEmail,
                        Configurations.SendGrid.DisplayName);

                    var to = new EmailAddress(email);

                    var htmlContent = content;

                    var msg = MailHelper.CreateSingleEmail(from, to, subject, "PlainText", htmlContent);

                    var response = await client.SendEmailAsync(msg);

                    if (response.StatusCode != HttpStatusCode.Accepted)
                    {
                        var error = await response.Body.ReadAsStringAsync();
                        Log.Error("Mailer send {0} {1} {2}", email, subject,
                            error);
                        return;
                    }

                    Log.Information("Mailer sent {0} {1} {2}", email, subject, response.StatusCode);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Mailer send {0} {1}", email, subject);
                }
            });
        }

        protected override void SendMail(string subject, string content, IEnumerable<string> emails)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = new SendGridClient(Configurations.SendGrid.ApiKey);
                    var from = new EmailAddress(Configurations.SendGrid.SenderEmail,
                        Configurations.SendGrid.DisplayName);

                    var to = emails.Select(x => new EmailAddress(x)).ToList();

                    var htmlContent = content;

                    var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, to, subject, "PlainText",
                        htmlContent);

                    var response = await client.SendEmailAsync(msg);

                    if (response.StatusCode != HttpStatusCode.Accepted)
                    {
                        var error = await response.Body.ReadAsStringAsync();
                        Log.Error("Mailer send {0} {1} {2}", "emails", subject,
                            error);
                        return;
                    }

                    Log.Information("Mailer sent {0} {1} {2}", "emails", subject, response.StatusCode);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Mailer send {0} {1}", "emails", subject);
                }
            });
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

        public async Task SendDepositReceive(string email, string username, DepositEntity deposit)
        {
            var mail = await GetMailTemplate(MailType.RECEIVE_DEPOSIT);

            mail.Content = mail.Content.Replace("{{username}}", username);
            mail.Content = mail.Content.Replace("{{asset}}", deposit.Asset);
            mail.Content = mail.Content.Replace("{{amount}}", deposit.Amount.ToCurrencyString());

            SendMail(mail.Subject, mail.Content, email);
        }

        public async Task SendWithdrawAlert(string email, string username, WithdrawEntity withdraw)
        {
            var mail = await GetMailTemplate(MailType.WITHDRAW_ALERT);

            mail.Content = mail.Content.Replace("{{username}}", username);
            mail.Content = mail.Content.Replace("{{asset}}", withdraw.Asset);
            mail.Content = mail.Content.Replace("{{address}}",
                string.IsNullOrEmpty(withdraw.AddressTag)
                    ? withdraw.Address
                    : $"{withdraw.Address}:{withdraw.AddressTag}");

            mail.Content = mail.Content.Replace("{{amount}}", withdraw.Amount.ToCurrencyString());

            SendMail(mail.Subject, mail.Content, email);
        }

        public async Task SendKycApprove(string email, string username)
        {
            var mail = await GetMailTemplate(MailType.KYC_APPROVE);

            mail.Content = mail.Content.Replace("{{username}}", username);

            SendMail(mail.Subject, mail.Content, email);
        }

        public async Task SendKycReject(string email, string username, string note)
        {
            var mail = await GetMailTemplate(MailType.KYC_REJECT);

            mail.Content = mail.Content.Replace("{{username}}", username);
            mail.Content = mail.Content.Replace("{{note}}", note);

            SendMail(mail.Subject, mail.Content, email);
        }
    }
}
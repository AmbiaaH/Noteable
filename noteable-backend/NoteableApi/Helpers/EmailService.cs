using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;

namespace NoteableApi.Helpers
{
    // service that sends emails using SMTP with settings from configuration
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) // injects configuration to access email settings

        {
            _config = config;
        }
        
        // sends an email with subject and HTML content to a given address
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            Console.WriteLine("EmailService triggered");
            Console.WriteLine($"To: {toEmail}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body: {htmlMessage}");

            var smtpClient = new SmtpClient(_config["Email:Smtp:Host"])
            {
                Port = int.Parse(_config["Email:Smtp:Port"]),
                Credentials = new NetworkCredential(
                    _config["Email:Smtp:Username"],
                    _config["Email:Smtp:Password"]
                ),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Email:From"]),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine("Email sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}

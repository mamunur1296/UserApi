using MailKit.Net.Smtp;
using MimeKit;
using User.Management.Survice.Models;

namespace User.Management.Survice.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfiguration _configuration;

        public EmailService(EmailConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public void SendEmail(Message message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Email",_configuration.From));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Text) {Text=message.Body };


            using var client = new SmtpClient();
            try
            {
                client.Connect(_configuration.SmtpServer, _configuration.Port, true);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(_configuration.UserName, _configuration.Password);
            }
            catch 
            {
                throw;
            }
            finally
            {
                client.Send(emailMessage);
                client.Disconnect(true);
                client.Dispose();
            }
        }
    }
}

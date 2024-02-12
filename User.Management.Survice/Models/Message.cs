
using System.Collections.Generic;
using MimeKit;

namespace User.Management.Survice.Models
{
    public class Message
    {
        public List<MailboxAddress>? To { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }

        public Message(IEnumerable<string> to, string subject, string body)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => new MailboxAddress("Email",x)));
            Subject = subject;
            Body = body;
        }
    }
}

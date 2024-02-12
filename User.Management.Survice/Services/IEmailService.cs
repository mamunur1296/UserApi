
using User.Management.Survice.Models;

namespace User.Management.Survice.Services
{
    public interface IEmailService
    {
        void SendEmail(Message message);
    }
}

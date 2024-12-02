using API.Services;

namespace API.Interfaces
{
    public interface IMailService
    {
        Task<bool> SendMailAsync(MailRequest mailRequest);
    }
}

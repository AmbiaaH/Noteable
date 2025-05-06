using System.Threading.Tasks;

// defines a contract for sending emails with HTML content
namespace NoteableApi.Helpers
{
    public interface IEmailService
    {
        // sends an email to a given address with subject and HTML message
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}

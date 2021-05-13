using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace MyCourse.Models.Services.Infrastructure
{
    public interface IEmailClient : IEmailSender
    {
        Task SendEmailAsync(string recipientEmail, string replyToEmail, string subject, string htmlMessage);
    }
}

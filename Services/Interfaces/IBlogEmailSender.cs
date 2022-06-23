using Microsoft.AspNetCore.Identity.UI.Services;

namespace Portfolio.Services.Interfaces;

public interface IBlogEmailSender : IEmailSender
{
    Task SendContactEmailAsync(string emailFrom, string name, string subject, string htmlMessage);
    new Task SendEmailAsync(string emailTo, string subject, string htmlMessage);
}
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Portfolio.Services.Interfaces;
using Portfolio.ViewModels;

namespace Portfolio.Services;

public class EmailService : IBlogEmailSender
{
    private readonly MailSettingsViewModel _settingsViewModel;

    public EmailService(IOptions<MailSettingsViewModel> settingsViewModel)
    {
        _settingsViewModel = settingsViewModel.Value;
    }

    public async Task SendEmailAsync(string emailTo, string subject, string htmlMessage)
    {
        //Write email
        var email = new MimeMessage
        {
            Sender = MailboxAddress.Parse(_settingsViewModel.Mail),
            To = { MailboxAddress.Parse(emailTo) },
            From = { MailboxAddress.Parse("noreply@mikemrobinsondev.com") },
            Subject = subject
        };

        //define email body using passed in HTML string.
        var builder = new BodyBuilder
        {
            HtmlBody = htmlMessage
        };

        //use builder to define email body.
        email.Body = builder.ToMessageBody();

        //Send email
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settingsViewModel.Host, _settingsViewModel.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settingsViewModel.Mail, _settingsViewModel.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendContactEmailAsync(string emailFrom, string name, string subject, string htmlMessage)
    {
        //Write email
        var email = new MimeMessage
        {
            Sender = MailboxAddress.Parse(_settingsViewModel.Mail),
            To = { MailboxAddress.Parse("bigmike2238@yahoo.com") },
            From = { MailboxAddress.Parse("noreply@mikemrobinsondev.com") },
            Subject = subject
        };

        //define email body using passed in HTML string.
        var builder = new BodyBuilder
        {
            HtmlBody = htmlMessage
        };

        //use builder to define email body.
        email.Body = builder.ToMessageBody();

        //Send email
        var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settingsViewModel.Host, _settingsViewModel.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settingsViewModel.Mail, _settingsViewModel.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
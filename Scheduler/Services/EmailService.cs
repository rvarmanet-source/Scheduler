using MailKit.Net.Smtp;
using MimeKit;

namespace Scheduler.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _emailAddress;
    private readonly string _emailPassword;

    public EmailService(string smtpServer, int smtpPort, string emailAddress, string emailPassword)
    {
        _smtpServer = smtpServer;
        _smtpPort = smtpPort;
        _emailAddress = emailAddress;
        _emailPassword = emailPassword;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Scheduler", _emailAddress));
            message.To.Add(new MailboxAddress("Recipient", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailAddress, _emailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Email sent successfully to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error sending email: {ex.Message}");
        }
    }
}

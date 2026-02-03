using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlMessage)
    {
        logger.LogInformation(
            "EMAIL (mock) -> To: {To} | Subject: {Subject} | Body: {Body}",
            toEmail,
            subject,
            htmlMessage);

        Console.WriteLine($"EMAIL (mock) -> To: {toEmail} | Subject: {subject} | Body: {htmlMessage}");

        return Task.CompletedTask;
    }
}

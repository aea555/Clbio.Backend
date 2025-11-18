using Clbio.Abstractions.Interfaces.Services;

namespace Clbio.Tests.Helpers
{
    public class FakeEmailSender : IEmailSender
    {
        public List<(string Email, string Subject, string Body)> SentEmails { get; } = [];

        public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            SentEmails.Add((toEmail, subject, htmlBody));
            return Task.CompletedTask;
        }
    }
}

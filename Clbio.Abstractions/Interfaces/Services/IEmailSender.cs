namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default);
    }
}

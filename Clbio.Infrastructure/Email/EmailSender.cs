using Clbio.Abstractions.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Clbio.Infrastructure.Email
{
    public sealed class MailerSendEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly string _apiToken;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string? _replyTo;
        private readonly ILogger<MailerSendEmailSender>? _logger;

        public MailerSendEmailSender(
            IConfiguration config,
            ILogger<MailerSendEmailSender>? logger = null)
        {
            _logger = logger;

            var section = config.GetSection("Email");
            _apiToken = section["MailerSendApiToken"]
                        ?? throw new InvalidOperationException("Email:MailerSendApiToken missing");

            _fromEmail = section["FromEmail"]
                        ?? throw new InvalidOperationException("Email:FromEmail missing");

            _fromName = section["FromName"] ?? "No-Reply";
            _replyTo = section["ReplyToEmail"];

            _http = new HttpClient
            {
                BaseAddress = new Uri("https://api.mailersend.com/v1/")
            };

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiToken);

            _http.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default)
        {
            var payload = new
            {
                from = new { email = _fromEmail, name = _fromName },
                to = new[] { new { email = toEmail } },
                subject,
                html = htmlBody,
                reply_to = _replyTo != null ? new[] { new { email = _replyTo } } : null
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("email", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger?.LogError("MailerSend API error: {Status} - {Body}",
                        response.StatusCode, body);

                    throw new Exception($"MailerSend send failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }
    }
}

using Clbio.Abstractions.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Clbio.Infrastructure.Email
{
    public sealed class MailTrapEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly string _apiToken;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<MailTrapEmailSender>? _logger;

        public MailTrapEmailSender(
            IConfiguration config,
            ILogger<MailTrapEmailSender>? logger = null)
        {
            _logger = logger;

            var section = config.GetSection("Email");
            _apiToken = section["MailtrapApiToken"]
                        ?? throw new InvalidOperationException("Email:MailtrapApiToken missing");

            _fromEmail = section["FromEmail"]
                        ?? throw new InvalidOperationException("Email:FromEmail missing");

            _fromName = section["FromName"] ?? "Clbio System";

            _http = new HttpClient
            {
                BaseAddress = new Uri("https://send.api.mailtrap.io/api/")
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
                html = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("send", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger?.LogError("Mailtrap API error: {Status} - {Body}",
                        response.StatusCode, body);

                    throw new Exception($"Mailtrap send failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send email via Mailtrap to {Email}", toEmail);
                throw;
            }
        }
    }
}
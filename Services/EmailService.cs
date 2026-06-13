using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace meow.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly Dictionary<string, (string Host, int Port, bool UseSsl)> ProviderPresets =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Brevo"] = ("smtp-relay.brevo.com", 587, true),
                ["Sendinblue"] = ("smtp-relay.brevo.com", 587, true),
                ["Gmail"] = ("smtp.gmail.com", 587, true),
                ["Mailpit"] = ("localhost", 1025, false),
            };

        public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ResolveApiKey()) || !string.IsNullOrWhiteSpace(ResolveHost());

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            var fromName = _config["Smtp:FromName"] ?? "meow E-Księgarnia";
            var from = ResolveFromAddress();

            if (!TryParseMailbox(to, out _))
            {
                _logger.LogWarning("Pominięto wysyłkę — nieprawidłowy adres odbiorcy: {To}", to);
                return;
            }

            if (!TryParseMailbox(from, out _, fromName))
            {
                _logger.LogWarning("Pominięto wysyłkę — nieprawidłowy adres nadawcy (Smtp:From): {From}", from);
                return;
            }

            var apiKey = ResolveApiKey();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                await SendViaBrevoApiAsync(fromName, from, to, subject, htmlBody, apiKey, ct);
                return;
            }

            var host = ResolveHost();
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogInformation(
                    "[MAIL-DEV] Brak SMTP — symulacja wysyłki.\nDo: {To}\nTemat: {Subject}\n{Body}",
                    to, subject, htmlBody);
                return;
            }

            await SendViaSmtpAsync(fromName, from, to, subject, htmlBody, host, ct);
        }

        public async Task SendOrderConfirmationAsync(
            string email, string clientName, string trackingNumber,
            IEnumerable<string> items, CancellationToken ct = default)
        {
            var lista = string.Join("<br/>", items.Select(i => $"• {i}"));
            var body = $@"
                <h2>meow — potwierdzenie zamówienia</h2>
                <p>Witaj <strong>{clientName}</strong>,</p>
                <p>Twoje zamówienie zostało przyjęte.</p>
                <p><strong>Numer paczki:</strong> {trackingNumber}</p>
                <p><strong>Pozycje:</strong><br/>{lista}</p>
                <p>Status śledzenia sprawdzisz w sekcji Moje Konto.</p>
                <hr/><small>E-Księgarnia meow</small>";

            await SendAsync(email, "Potwierdzenie zamówienia meow", body, ct);
        }

        private async Task SendViaBrevoApiAsync(
            string fromName, string from, string to, string subject, string htmlBody,
            string apiKey, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("Brevo");
            client.DefaultRequestHeaders.Remove("api-key");
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var payload = new BrevoEmailRequest
            {
                Sender = new BrevoContact { Name = fromName, Email = from },
                To = [new BrevoContact { Email = to }],
                Bcc = from != to ? [new BrevoContact { Email = from }] : null,
                Subject = subject,
                HtmlContent = htmlBody,
                TextContent = StripHtml(htmlBody)
            };

            var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Brevo API błąd {Status}: {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Brevo API: {response.StatusCode}");
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation(
                "Wysłano e-mail (Brevo API) do {To}, od {From}, temat: {Subject}, odpowiedź: {Body}",
                to, from, subject, body);
        }

        private static string StripHtml(string html) =>
            System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
                .Replace("&nbsp;", " ")
                .Trim();

        private async Task SendViaSmtpAsync(
            string fromName, string from, string to, string subject, string htmlBody,
            string host, CancellationToken ct)
        {
            TryParseMailbox(from, out var fromAddress, fromName);
            TryParseMailbox(to, out var toAddress);

            var port = ResolvePort(host);
            var useSsl = ResolveUseSsl(host, port);

            var message = new MimeMessage();
            message.From.Add(fromAddress);
            message.To.Add(toAddress);
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient { Timeout = 15000 };
            var socketOptions = useSsl
                ? (port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;

            await client.ConnectAsync(host, port, socketOptions, ct);

            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Password"];
            if (!string.IsNullOrEmpty(user))
                await client.AuthenticateAsync(user, pass, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Wysłano e-mail (SMTP) do {To}, temat: {Subject}", to, subject);
        }

        private string? ResolveApiKey()
        {
            var apiKey = _config["Smtp:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                return apiKey.Trim();

            return null;
        }

        private string? ResolveHost()
        {
            var host = _config["Smtp:Host"];
            if (!string.IsNullOrWhiteSpace(host))
                return host.Trim();

            var provider = _config["Smtp:Provider"];
            if (!string.IsNullOrWhiteSpace(provider) &&
                ProviderPresets.TryGetValue(provider.Trim(), out var preset))
                return preset.Host;

            return null;
        }

        private int ResolvePort(string host)
        {
            if (int.TryParse(_config["Smtp:Port"], out var port))
                return port;

            foreach (var preset in ProviderPresets.Values)
            {
                if (string.Equals(preset.Host, host, StringComparison.OrdinalIgnoreCase))
                    return preset.Port;
            }

            return 587;
        }

        private bool ResolveUseSsl(string host, int port)
        {
            if (_config.GetSection("Smtp:UseSsl").Exists())
                return _config.GetValue<bool>("Smtp:UseSsl");

            foreach (var preset in ProviderPresets.Values)
            {
                if (string.Equals(preset.Host, host, StringComparison.OrdinalIgnoreCase))
                    return preset.UseSsl;
            }

            return port != 1025;
        }

        private string ResolveFromAddress()
        {
            var from = _config["Smtp:From"];
            if (!string.IsNullOrWhiteSpace(from))
                return from.Trim();

            var user = _config["Smtp:User"];
            if (!string.IsNullOrWhiteSpace(user) && user.Contains('@'))
                return user.Trim();

            return "noreply@meow.pl";
        }

        private static bool TryParseMailbox(string address, out MailboxAddress mailbox, string? displayName = null)
        {
            mailbox = null!;
            if (string.IsNullOrWhiteSpace(address))
                return false;

            address = address.Trim();

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                try
                {
                    mailbox = new MailboxAddress(displayName.Trim(), address);
                    return mailbox.Address.Contains('@');
                }
                catch (ParseException)
                {
                    return false;
                }
            }

            return MailboxAddress.TryParse(address, out mailbox);
        }

        private sealed class BrevoEmailRequest
        {
            [JsonPropertyName("sender")]
            public BrevoContact Sender { get; set; } = new();

            [JsonPropertyName("to")]
            public List<BrevoContact> To { get; set; } = [];

            [JsonPropertyName("bcc")]
            public List<BrevoContact>? Bcc { get; set; }

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = "";

            [JsonPropertyName("htmlContent")]
            public string HtmlContent { get; set; } = "";

            [JsonPropertyName("textContent")]
            public string? TextContent { get; set; }
        }

        private sealed class BrevoContact
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; } = "";
        }
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace meow.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            var host = _config["Smtp:Host"];
            var from = _config["Smtp:From"] ?? "noreply@meow.pl";
            var fromName = _config["Smtp:FromName"] ?? "meow E-Księgarnia";

            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogInformation(
                    "[MAIL-DEV] Brak SMTP — symulacja wysyłki.\nDo: {To}\nTemat: {Subject}\n{Body}",
                    to, subject, htmlBody);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            await client.ConnectAsync(host, port,
                _config.GetValue<bool>("Smtp:UseSsl") ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Password"];
            if (!string.IsNullOrEmpty(user))
                await client.AuthenticateAsync(user, pass, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Wysłano e-mail do {To}, temat: {Subject}", to, subject);
        }

        public async Task SendOrderConfirmationAsync(
            string email, string clientName, string trackingNumber,
            IEnumerable<string> items, CancellationToken ct = default)
        {
            var lista = string.Join("<br/>", items.Select(i => $"• {i}"));
            var body = $@"
                <h2>🐾 meow — potwierdzenie zamówienia</h2>
                <p>Witaj <strong>{clientName}</strong>,</p>
                <p>Twoje zamówienie zostało przyjęte.</p>
                <p><strong>Numer paczki:</strong> {trackingNumber}</p>
                <p><strong>Pozycje:</strong><br/>{lista}</p>
                <p>Status śledzenia sprawdzisz w sekcji Moje Konto.</p>
                <hr/><small>E-Księgarnia meow</small>";

            await SendAsync(email, "Potwierdzenie zamówienia meow", body, ct);
        }
    }
}

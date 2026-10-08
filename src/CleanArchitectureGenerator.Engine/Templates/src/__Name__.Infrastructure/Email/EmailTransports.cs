//#if LocalAuth
using System.Net;
using System.Net.Mail;
using System.Text;
using __Name__.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace __Name__.Infrastructure.Email;

/// <summary>E-postayı gerçekten ileten bileşen. Başka bir sağlayıcı (SendGrid, Amazon SES…) için yeni bir uygulama yazın.</summary>
internal interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Yalnızca Development ortamında kullanılır: e-posta gönderilmez, içeriği loga yazılır. Doğrulama ve parola
/// sıfırlama bağlantılarını konsoldan kopyalayabilirsiniz.
/// </summary>
internal sealed class DevelopmentEmailTransport(ILogger<DevelopmentEmailTransport> logger) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "[GELİŞTİRME] E-posta gönderilmedi, içeriği aşağıda.\nAlıcı: {To}\nKonu: {Subject}\n{Body}",
            message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}

/// <summary>SMTP ile gönderim. Bağlantı her zaman TLS ile şifrelenir (587 numaralı port, STARTTLS).</summary>
internal sealed class SmtpEmailTransport(IOptions<SmtpOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value;
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrEmpty(smtp.Username))
        {
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        }

        // Gövde düz metindir; kullanıcıdan gelen değerler HTML olarak yorumlanamaz.
        using var mail = new MailMessage(smtp.From, message.To, message.Subject, message.Body)
        {
            IsBodyHtml = false,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        await client.SendMailAsync(mail, cancellationToken);
    }
}
//#endif

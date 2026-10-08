//#if LocalAuth
using System.Net.Mail;

namespace __Name__.Infrastructure.Email;

/// <summary>
/// SMTP ayarları ("Smtp" bölümü). Development dışındaki ortamlarda zorunludur; eksikse uygulama açılmaz.
/// Kullanıcı adı ve parola yapılandırma dosyasına yazılmaz: ortam değişkeni (Smtp__Password) ya da anahtar kasası.
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>Gönderen adresi, ör. "Uygulama &lt;noreply@ornek.com&gt;".</summary>
    public string From { get; set; } = string.Empty;

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Host) && Port is > 0 and <= 65535 && MailAddress.TryCreate(From, out _);

    // Parola içerdiği için ToString değerleri göstermez.
    public override string ToString() => nameof(SmtpOptions);
}
//#endif

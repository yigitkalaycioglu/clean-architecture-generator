//#if LocalAuth
namespace __Name__.Application.Abstractions.Email;

/// <summary>
/// E-posta gönderir. Uygulama mesajı kuyruğa alır ve hemen döner, gönderim arka planda yapılır; böylece
/// yanıt süresi bir e-posta adresinin kayıtlı olup olmadığını ele vermez.
/// </summary>
public interface IEmailSender
{
    ValueTask SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <param name="Body">Düz metin gövde. Kullanıcı girdisi HTML olarak yorumlanmasın diye HTML kullanılmaz.</param>
public sealed record EmailMessage(string To, string Subject, string Body)
{
    // Doğrulama ve parola sıfırlama kodları içerebilir; ToString içeriği göstermez.
    public override string ToString() => $"{nameof(EmailMessage)} {{ Subject = {Subject} }}";
}
//#endif

//#if LocalAuth
using System.Collections.Concurrent;
using __Name__.Application.Abstractions.Email;
using Microsoft.AspNetCore.WebUtilities;

namespace __Name__.IntegrationTests;

/// <summary>Gönderilen e-postaları bellekte tutar; testler doğrulama ve sıfırlama bağlantılarını buradan okur.</summary>
public sealed class TestEmailSender : IEmailSender
{
    public const string FrontendBaseUrl = "https://app.test";

    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public ValueTask SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> MessagesTo(string email) =>
        _messages.Where(message => string.Equals(message.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Adrese gönderilen son e-postadaki bağlantının sorgu parametreleri (userId, code, email…).</summary>
    public IReadOnlyDictionary<string, string> LastLinkParameters(string email)
    {
        var body = MessagesTo(email)[^1].Body;
        var start = body.IndexOf(FrontendBaseUrl, StringComparison.Ordinal);
        Assert.True(start >= 0, "E-postada bağlantı bulunamadı.");

        var end = body.IndexOfAny([' ', '\n', '\r'], start);
        var link = new Uri(end < 0 ? body[start..] : body[start..end]);
        return QueryHelpers.ParseQuery(link.Query).ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
    }
}
//#endif

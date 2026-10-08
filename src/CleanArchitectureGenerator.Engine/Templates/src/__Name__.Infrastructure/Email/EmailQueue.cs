//#if LocalAuth
using System.Threading.Channels;
using __Name__.Application.Abstractions.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace __Name__.Infrastructure.Email;

/// <summary>
/// E-postaları bellekteki bir kuyruğa alır; <see cref="EmailDispatcher"/> arka planda gönderir. İstek, e-posta
/// sunucusunu beklemeden döner: hem yanıt süresi sabit kalır hem de yavaş bir sunucu API'yi kilitlemez.
/// </summary>
internal sealed class EmailQueue(ILogger<EmailQueue> logger) : IEmailSender
{
    // Sınırlı kapasite: kayıt formu kötüye kullanılsa bile bellek tükenmez (ayrıca hız sınırı vardır).
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(1000) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public ValueTask SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!_channel.Writer.TryWrite(message))
        {
            logger.LogError("E-posta kuyruğu dolu, mesaj gönderilmedi: {Subject}", message.Subject);
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class EmailDispatcher(EmailQueue queue, IEmailTransport transport, ILogger<EmailDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await transport.SendAsync(message, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "E-posta gönderilemedi: {Subject}", message.Subject);
            }
        }
    }
}
//#endif

using System.Threading.Channels;
using Infrastructure.Email.Models;

namespace Infrastructure.Email.Queue;

/// <summary>
/// A thread-safe, bounded in-memory queue for outgoing email messages.
/// Uses a <see cref="Channel{T}"/> to decouple producers (e.g. application services)
/// from the consumer (<c>EmailSenderService</c>) without blocking the caller.
/// </summary>
public sealed class EmailQueue : IEmailQueue
{
    /// <summary>
    /// Bounded channel with a capacity of 100 pending messages.
    /// <list type="bullet">
    ///   <item><description><b>FullMode = Wait</b>: back-pressures the writer when the channel is full instead of dropping messages.</description></item>
    ///   <item><description><b>SingleReader = true</b>: allows the channel to apply reader-side optimisations because only <c>EmailSenderService</c> reads from it.</description></item>
    ///   <item><description><b>SingleWriter = false</b>: multiple application services may enqueue emails concurrently.</description></item>
    /// </list>
    /// </summary>
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    /// <summary>
    /// Exposes the read end of the channel so that <c>EmailSenderService</c>
    /// can consume messages without gaining write access.
    /// </summary>
    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    /// <summary>
    /// Asynchronously enqueues an <see cref="EmailMessage"/> for delivery.
    /// The call will yield (back-pressure) if the channel has reached its capacity of 100 items
    /// and will resume once the consumer frees up space.
    /// </summary>
    /// <param name="message">The email message to enqueue.</param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> that can be used to cancel the wait
    /// when the channel is full.
    /// </param>
    public async ValueTask EnqueueAsync(
        EmailMessage message,
        CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(message, ct);
    }
}
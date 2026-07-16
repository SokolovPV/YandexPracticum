using Confluent.Kafka;
using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Infrastructure.Options;
using EventFlow.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventFlow.Bookings.Infrastructure.Services;

/// <summary>
/// Kafka producer события BookingConfirmed
/// </summary>
public sealed class BookingConfirmedProducer : IBookingConfirmedProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<BookingConfirmedProducer> _logger;

    public BookingConfirmedProducer(
        IOptions<KafkaOptions> options,
        ILogger<BookingConfirmedProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    public async Task PublishAsync(string topic, string key, string payload, CancellationToken ct)
    {
        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = payload
            },
            ct);

        _logger.LogInformation("Сообщение опубликовано. Topic={Topic}, Key={Key}", topic, key);
    }
}

using System.Text.Json;
using Confluent.Kafka;
using EventFlow.Entities.Brokers;
using EventFlow.Entities.Redis;
using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Domain.Exceptions;
using EventFlow.Events.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventFlow.Events.Infrastructure.Services;

public class BookingConfirmedConsumer(
    IServiceScopeFactory scopeFactory,
    ICacheService cache,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            GroupId = kafkaOptions.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicNames.BookingConfirmed);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                    continue;

                var message = JsonSerializer.Deserialize<BookingConfirmed>(consumeResult.Message.Value);
                await HandleMessageAsync(message, stoppingToken);
                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке сообщения BookingConfirmed");
            }
        }
    }

    public async Task HandleMessageAsync(BookingConfirmed? message, CancellationToken stoppingToken)
    {
        if (message == null)
        {
            logger.LogWarning($"Получено пустое сообщение {nameof(BookingConfirmed)}");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var processedRepository = scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        if (await processedRepository.ExistsAsync(message.MessageId, stoppingToken))
        {
            logger.LogInformation("Сообщение {MessageId} обработано ранее", message.MessageId);
            return;
        }

        // Пробуем забронировать места на мероприятие
        bool reserveSeatState;
        try
        {
            reserveSeatState = await eventService.TryReserveSeatAsync(message.EventId, stoppingToken);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex,
                "Событие {EventId} для сообщения {MessageId} не найдено. Помечаем сообщение как обработанное.",
                message.EventId,
                message.MessageId);

            await processedRepository.AddAsync(message.MessageId, stoppingToken);
            return;
        }
        catch (EventAlreadyStartedException ex)
        {
            logger.LogWarning(ex,
                "Событие {EventId} для сообщения {MessageId} уже началось, бронирование невозможно. Помечаем сообщение как обработанное.",
                message.EventId,
                message.MessageId);

            await processedRepository.AddAsync(message.MessageId, stoppingToken);
            return;
        }


        if (!reserveSeatState)
        {
            logger.LogWarning("Не удалось уменьшить количество свободных мест для EventId={EventId}. Cвободных мест нет.", message.EventId);
            return;
        }

        await processedRepository.AddAsync(message.MessageId, stoppingToken);
    }
}
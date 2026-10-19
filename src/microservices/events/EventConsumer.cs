using Confluent.Kafka;

namespace Events;

public class EventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventConsumer> _logger;

    public EventConsumer(IConfiguration configuration, ILogger<EventConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = _configuration["KAFKA_BROKERS"] ?? "kafka:9092",
                GroupId = "cinemaabyss-events",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            }).Build();
            consumer.Subscribe(new[] { "movie-events", "user-events", "payment-events" });
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        _logger.LogInformation("Обработано событие: topic={Topic}, partition={Partition}, offset={Offset}, message={Message}",
                            result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Value);
                        // Подтверждаем чтение после обработки.
                        consumer.Commit(result);
                    }
                    catch (KafkaException error)
                    {
                        _logger.LogWarning(error, "Ошибка чтения Kafka");
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            finally { consumer.Close(); }
        }, stoppingToken);
    }
}

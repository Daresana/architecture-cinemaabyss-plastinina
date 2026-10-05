using Confluent.Kafka;
using System.Text.Json;

namespace Events;

public class EventProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<EventProducer> _logger;

    public EventProducer(IConfiguration configuration, ILogger<EventProducer> logger)
    {
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = configuration["KAFKA_BROKERS"] ?? "kafka:9092",
            Acks = Acks.All,
            MessageTimeoutMs = 10000
        }).Build();
    }

    public async Task<object> SendAsync(string type, JsonElement payload, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString();
        var message = new { id, type, timestamp = DateTimeOffset.UtcNow, payload };
        // Ответ возвращается после подтверждения записи брокером.
        var result = await _producer.ProduceAsync($"{type}-events", new Message<string, string>
        {
            Key = id,
            Value = JsonSerializer.Serialize(message)
        }, cancellationToken);
        _logger.LogInformation("Событие {Id} записано в {Topic}, partition={Partition}, offset={Offset}",
            id, result.Topic, result.Partition.Value, result.Offset.Value);
        return new { status = "success", partition = result.Partition.Value, offset = result.Offset.Value, @event = message };
    }

    public void Dispose() => _producer.Dispose();
}

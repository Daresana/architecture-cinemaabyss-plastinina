using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Events;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventProducer _producer;
    private readonly ILogger<EventsController> _logger;

    public EventsController(EventProducer producer, ILogger<EventsController> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = true });

    [HttpPost("{type}")]
    public async Task<IActionResult> Create(string type, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        if (type is not ("movie" or "user" or "payment"))
            return NotFound();
        if (!IsValid(type, payload))
            return BadRequest(new { error = "Invalid event data" });
        try
        {
            var result = await _producer.SendAsync(type, payload, cancellationToken);
            return StatusCode(201, result);
        }
        catch (KafkaException error)
        {
            _logger.LogError(error, "Не удалось записать событие");
            return StatusCode(500, new { error = "Kafka is unavailable" });
        }
    }

    private static bool IsValid(string type, JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return false;
        bool Integer(string key) => payload.TryGetProperty(key, out var item)
            && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out _);
        bool Text(string key) => payload.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String;
        bool Timestamp() => Text("timestamp") && DateTimeOffset.TryParse(payload.GetProperty("timestamp").GetString(), out _);
        return type switch
        {
            "movie" => Integer("movie_id") && Text("title") && Text("action"),
            "user" => Integer("user_id") && Text("action") && Timestamp(),
            "payment" => Integer("payment_id") && Integer("user_id") && Text("status") && Timestamp()
                && payload.TryGetProperty("amount", out var amount) && amount.ValueKind == JsonValueKind.Number,
            _ => false
        };
    }
}

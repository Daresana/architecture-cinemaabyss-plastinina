using Events;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{builder.Configuration["PORT"] ?? "8082"}");
builder.Services.AddSingleton<EventProducer>();
builder.Services.AddHostedService<EventConsumer>();
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
    options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(new { error = "Invalid event data" }));

var app = builder.Build();
app.MapControllers();
app.Run();

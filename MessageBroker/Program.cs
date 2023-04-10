using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=MessageBroker.db"));

var app = builder.Build();

app.UseHttpsRedirection();

// Create Topic
app.MapPost("api/topics", async (AppDbContext context, Topic topic) =>
{
    await context.Topics.AddAsync(topic);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{topic.Id}", topic);
});

// Return all Topics
app.MapGet("api/topics", async (AppDbContext context) =>
{
    var topics = await context.Topics.ToListAsync();

    return Results.Ok(topics);
});

// Publish Message
app.MapPost("api/topics/{id}/messages", async (AppDbContext context, int id, Message message) =>
{
    bool topics = await context.Topics.AnyAsync(p => p.Id == id);
    if (!topics)
        return Results.NotFound("Topic not found");

    var subs = context.Subscriptions.Where(p => p.TopicId == id);
    if (subs.Count() == 0)
        return Results.NotFound("There are no subscriptions for this topic");

    foreach (var sub in subs)
    {
        var msg = new Message
        {
            TopicMessage = message.TopicMessage,
            SubscriptionId = sub.Id,
            ExpiresAfter = message.ExpiresAfter,
            MessageStatus = message.MessageStatus
        };

        await context.Messages.AddAsync(msg);
    }
    await context.SaveChangesAsync();

    return Results.Ok("Message has been published");
});


app.Run();
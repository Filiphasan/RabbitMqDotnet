using System.Text;
using Carter;
using Microsoft.AspNetCore.Mvc;
using Publisher.Models;
using Publisher.Services.Interfaces;
using RabbitMQ.Client;
using Shared.Common.Constants;
using Shared.Common.QueueModels;

namespace Publisher.Endpoints;

public class QueueEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/queues")
            .WithTags("Queue");

        group.MapPost("/send-with-bad-but-basic-way", SendWithBadButBasicWayAsync);
        group.MapPost("/send", SendAsync);
        group.MapPost("/publish", PublishAsync);
    }

    private static async Task<IResult> SendWithBadButBasicWayAsync(string message, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = configuration["Settings:RabbitMq:Host"]!,
            Port = Convert.ToInt32(configuration["Settings:RabbitMq:Port"]!),
            UserName = configuration["Settings:RabbitMq:User"]!,
            Password = configuration["Settings:RabbitMq:Password"]!,
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = configuration["Settings:RabbitMq:ConnectionName"]!
        };

        await using var connection = await connectionFactory.CreateConnectionAsync(configuration["RabbitMq:ConnectionName"]!, cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { { "x-message-id", Guid.NewGuid().ToString() } },
            DeliveryMode = DeliveryModes.Persistent,
            Priority = 2
        };
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(exchange: "", routingKey: "basic.with.bad.way.send.queue", mandatory: false, basicProperties: properties, body: body, cancellationToken);

        return Results.Ok();
    }

    private static async Task<IResult> SendAsync([FromBody] BasicSendRequest request, IRabbitMqService rabbitMqService, CancellationToken cancellationToken)
    {
        var queueModel = new QueueBasicModel
        {
            PickedNumber = request.PickedNumber,
            Message = request.Message
        };
        var sendModel = new SendMessageModel<QueueBasicModel>
        {
            Message = queueModel,
            QueueName = QueueConstant.QueueNames.BasicSendQueue,
        };
        await rabbitMqService.SendAsync(sendModel, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> PublishAsync([FromBody] BasicPublishRequest request, IRabbitMqService rabbitMqService, CancellationToken cancellationToken)
    {
        var queueModel = new QueueBasicModel
        {
            PickedNumber = request.PickedNumber,
            Message = request.Message
        };
        var publishModel = new PublishMessageModel<QueueBasicModel>
        {
            Message = queueModel,
            ExchangeName = QueueConstant.ExchangeNames.BasicPublishExchange,
            RoutingKey = QueueConstant.RoutingKeys.BasicPublishRoutingKey,
        };
        await rabbitMqService.PublishAsync(publishModel, cancellationToken);
        return Results.Ok();
    }
}
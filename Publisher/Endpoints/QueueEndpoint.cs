using Carter;
using Microsoft.AspNetCore.Mvc;
using Publisher.Models;
using Publisher.Services.Interfaces;
using Shared.Common.Constants;
using Shared.Common.QueueModels;

namespace Publisher.Endpoints;

public class QueueEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/queues")
            .WithTags("Queue");

        group.MapPost("/send", SendAsync);
        group.MapPost("/publish", PublishAsync);
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
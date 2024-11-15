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

    private static async Task<IResult> SendAsync([FromBody] BasicSendRequest request, IRabbitMqService rabbitMqService)
    {
        var queueModel = new QueueBasicModel
        {
            PickedNumber = request.PickedNumber,
            Message = request.Message
        };
        await rabbitMqService.SendAsync(queueModel, QueueConstant.QueueNames.BasicSendQueue, Guid.NewGuid().ToString());
        return Results.Ok();
    }

    private static async Task<IResult> PublishAsync([FromBody] BasicPublishRequest request, IRabbitMqService rabbitMqService)
    {
        var queueModel = new QueueBasicModel
        {
            PickedNumber = request.PickedNumber,
            Message = request.Message
        };
        await rabbitMqService.PublishAsync(queueModel, QueueConstant.ExchangeNames.BasicPublishExchange, QueueConstant.RoutingKeys.BasicPublishRoutingKey);
        return Results.Ok();
    }
}
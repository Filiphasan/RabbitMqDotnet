﻿using Consumer.Services.Interfaces;
using Shared.Common.Constants;
using Shared.Common.QueueModels;
using Shared.Services;

namespace Consumer.Consumers;

public class BasicSendConsumer : BaseConsumer<QueueBasicModel>
{
    public BasicSendConsumer(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(rabbitMqConnectionService, loggerFactory, scopeFactory)
    {
    }

    protected override async Task ConsumeAsync(QueueBasicModel message)
    {
        var scope = ScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMyService>();
        await service.DoBasicSendConsumerWorkAsync(message);
        scope.Dispose();
    }

    protected override void SetupConsumer()
    {
        QueueName = QueueConstant.QueueNames.BasicSendQueue;
        ExchangeName = QueueConstant.ExchangeNames.BasicSendExchange;
        RoutingKey = "";
        MaxRetryCount = 3;
        RetryDelayMs = 60_000;
    }
}
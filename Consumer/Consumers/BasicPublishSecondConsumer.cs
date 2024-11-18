using Consumer.Services.Interfaces;
using Shared.Common.Constants;
using Shared.Common.QueueModels;
using Shared.Services;

namespace Consumer.Consumers;

public class BasicPublishSecondConsumer : BaseConsumer<QueueBasicModel>
{
    public BasicPublishSecondConsumer(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(rabbitMqConnectionService, loggerFactory, scopeFactory)
    {
    }

    protected override async Task ConsumeAsync(QueueBasicModel message)
    {
        var scope = ScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMyService>();
        await service.DoBasicPublishSecondConsumerWorkAsync(message);
        scope.Dispose();
    }

    protected override void SetupConsumer()
    {
        QueueName = QueueConstant.QueueNames.BasicPublishSecondQueue;
        ExchangeName = QueueConstant.ExchangeNames.BasicPublishExchange;
        RoutingKey = QueueConstant.RoutingKeys.BasicPublishRoutingKey;
        UseRetry = true;
        MaxRetryCount = 2;
        RetryDelayMs = 60_000;
    }
}
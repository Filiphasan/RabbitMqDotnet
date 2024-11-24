using Consumer.Services.Interfaces;
using Shared.Common.Constants;
using Shared.Common.QueueModels;
using Shared.Services;

namespace Consumer.Consumers.Consumers;

public class BasicPublishFirstConsumer : BaseConsumer<QueueBasicModel>
{
    public BasicPublishFirstConsumer(RabbitMqConnectionService rabbitMqConnectionService, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(rabbitMqConnectionService, loggerFactory, scopeFactory)
    {
    }

    protected override async Task ConsumeAsync(QueueBasicModel message)
    {
        var scope = ScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMyService>();
        await service.DoBasicPublishFirstConsumerWorkAsync(message);
        scope.Dispose();
    }

    protected override void SetupConsumer()
    {
        QueueInfo.Name = QueueConstant.QueueNames.BasicPublishFirstQueue;
        ExchangeInfo.Name = QueueConstant.ExchangeNames.BasicPublishExchange;
        ExchangeInfo.RoutingKey = QueueConstant.RoutingKeys.BasicPublishRoutingKey;
        UseRetry = true;
        MaxRetryCount = 2;
        RetryDelayMs = 60_000;
    }
}
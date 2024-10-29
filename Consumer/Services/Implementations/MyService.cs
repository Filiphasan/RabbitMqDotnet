using Consumer.Services.Interfaces;
using Shared.Common.QueueModels;

namespace Consumer.Services.Implementations;

public class MyService(ILogger<MyService> logger) : IMyService
{
    public async Task DoBasicSendConsumerWorkAsync(QueueBasicModel model)
    {
        logger.LogInformation("Message received: {Message}", model.Message);
        await Task.Delay(1000); // Simulate work
        logger.LogInformation("Message processed: {Message}", model.Message);
    }

    public async Task DoBasicPublishFirstConsumerWorkAsync(QueueBasicModel model)
    {
        logger.LogInformation("Message received on first consumer: {Message}", model.Message);
        await Task.Delay(1000); // Simulate work
        logger.LogInformation("Message processed on first consumer: {Message}", model.Message);
    }

    public async Task DoBasicPublishSecondConsumerWorkAsync(QueueBasicModel model)
    {
        logger.LogInformation("Message received on second consumer: {Message}", model.Message);
        await Task.Delay(1000); // Simulate work
        logger.LogInformation("Message processed on second consumer: {Message}", model.Message);
    }
}
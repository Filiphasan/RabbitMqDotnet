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
}
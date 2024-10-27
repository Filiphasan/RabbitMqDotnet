using Shared.Common.QueueModels;

namespace Consumer.Services.Interfaces;

public interface IMyService
{
    Task DoBasicSendConsumerWorkAsync(QueueBasicModel model);
}
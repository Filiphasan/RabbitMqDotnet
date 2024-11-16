namespace Consumer.Consumers.Abstract;

public interface IBaseConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
}
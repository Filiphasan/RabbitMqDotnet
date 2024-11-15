using RabbitMQ.Client;

namespace Publisher.Services.Interfaces;

public interface IRabbitMqService
{
    Task SendAsync<T>(T message, string queueName, string messageId);
    Task PublishAsync<T>(T message, string exchangeName, string routingKey, string exchangeType = ExchangeType.Direct);
}
using RabbitMQ.Client;

namespace Publisher.Models;

public class PublishMessageModel<TModel> where TModel : class
{
    public TModel? Message { get; set; }
    public string ExchangeName { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Direct;
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public DeliveryModes DeliveryMode { get; set; } = DeliveryModes.Transient;
    public byte Priority { get; set; } = 0;
}

public class SendMessageModel<TModel> where TModel : class
{
    public TModel? Message { get; set; }
    public string QueueName { get; set; } = string.Empty;
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public DeliveryModes DeliveryMode { get; set; } = DeliveryModes.Transient;
    public byte Priority { get; set; } = 0;
}
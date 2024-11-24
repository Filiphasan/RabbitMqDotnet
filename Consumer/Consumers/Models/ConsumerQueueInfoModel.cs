namespace Consumer.Consumers.Models;

public class ConsumerQueueInfoModel
{
    public string Name { get; set; } = string.Empty;
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public IDictionary<string, object?>? Arguments { get; set; } = null;

    public static ConsumerQueueInfoModel Default => new();
}

public class ConsumerExchangeInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Direct;
    public bool Durable { get; set; } = true;
    public IDictionary<string, object?>? Arguments { get; set; } = null;
    
    public static ConsumerExchangeInfoModel Default => new();
}
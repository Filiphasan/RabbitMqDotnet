namespace Shared.Common.Constants;

public static class QueueConstant
{
    public static class QueueNames
    {
        public const string BasicSendQueue = "basic.send.queue";
        public const string BasicPublishFirstQueue = "basic.publish.first.queue";
        public const string BasicPublishSecondQueue = "basic.publish.second.queue";
    }

    public static class ExchangeNames
    {
        public const string BasicSendExchange = "basic.send.exchange";
        public const string BasicPublishExchange = "basic.publish.exchange";
    }

    public static class RoutingKeys
    {
        public const string BasicSendRoutingKey = "basic.send.routing.key";
        public const string BasicPublishRoutingKey = "basic.publish.routing.key";
    }
}
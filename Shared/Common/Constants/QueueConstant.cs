namespace Shared.Common.Constants;

public static class QueueConstant
{
    public static class QueueNames
    {
        public const string BasicSendQueue = "basic.send.queue";
    }

    public static class ExchangeNames
    {
        public const string BasicSendExchange = "basic.send.exchange";
    }

    public static class RoutingKeys
    {
        public const string BasicSendRoutingKey = "basic.send.routing.key";
    }
}
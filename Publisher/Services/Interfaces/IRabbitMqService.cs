using Publisher.Models;
using RabbitMQ.Client;

namespace Publisher.Services.Interfaces;

public interface IRabbitMqService
{
    Task SendAsync<T>(SendMessageModel<T> message, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync<T>(PublishMessageModel<T> message, CancellationToken cancellationToken = default) where T : class;
}
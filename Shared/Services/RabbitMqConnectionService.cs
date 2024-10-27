using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Services;

public class RabbitMqConnectionService(IConnectionFactory connectionFactory, ILogger<RabbitMqConnectionService> logger)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private IConnection? _connection;

    private bool IsConnected => _connection is { IsOpen: true };

    public async Task<IModel> GetChannelAsync()
    {
        if (!IsConnected)
        {
            await HandleConnectionAsync();
        }

        var channel = _connection!.CreateModel();
        return channel;
    }

    private async Task HandleConnectionAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (IsConnected)
            {
                return;
            }

            if (_connection != null)
            {
                _connection.ConnectionShutdown -= OnConnectionShutdown;
                _connection.Dispose();
            }

            _connection = connectionFactory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        logger.LogError("RabbitMQ connection shutdown. Reason: {Reason}", e.ReplyText);
        HandleConnectionAsync().Wait();
    }
}
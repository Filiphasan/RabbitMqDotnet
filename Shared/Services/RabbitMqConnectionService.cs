using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;

namespace Shared.Services;

public class RabbitMqConnectionService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly AsyncPolicy _connectionRetryPolicy;

    private IConnection? _connection;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqConnectionService> _logger;

    public RabbitMqConnectionService(IConnectionFactory connectionFactory, ILogger<RabbitMqConnectionService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        
        _connectionRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timeSpan, _, retryCount) =>
            {
                _logger.LogError(exception, "RabbitMQ connection failed, retrying in {TimeOut}ms RetryCount: {RetryCount}", timeSpan.TotalMilliseconds, retryCount);
            });
    }

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

            await _connectionRetryPolicy.ExecuteAsync(() =>
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.Dispose();
                }

                _connection = _connectionFactory.CreateConnection("ProjectName");
                _connection.ConnectionShutdown += OnConnectionShutdown;

                return Task.CompletedTask;
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogError("RabbitMQ connection shutdown. Reason: {Reason}", e.ReplyText);
        HandleConnectionAsync().GetAwaiter().GetResult();
    }
}
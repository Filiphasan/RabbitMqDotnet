using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Common.Models;

namespace Shared.Services;

public class RabbitMqConnectionService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly AsyncPolicy _connectionRetryPolicy;

    private IConnection? _connection;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqConnectionService> _logger;
    private readonly ProjectSetting _projectSetting;

    public RabbitMqConnectionService(IConnectionFactory connectionFactory, ILogger<RabbitMqConnectionService> logger, ProjectSetting projectSetting)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _projectSetting = projectSetting;

        _connectionRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timeSpan, _, retryCount) =>
            {
                _logger.LogError(exception, "RabbitMQ connection failed, retrying in {TimeOut}ms RetryCount: {RetryCount}", timeSpan.TotalMilliseconds, retryCount);
            });
    }

    private bool IsConnected => _connection is { IsOpen: true };

    public async Task<IChannel> GetChannelAsync()
    {
        if (!IsConnected)
        {
            await HandleConnectionAsync();
        }

        var channel = await _connection!.CreateChannelAsync();
        return channel;
    }

    private async Task HandleConnectionAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            if (IsConnected)
            {
                return;
            }

            await _connectionRetryPolicy.ExecuteAsync(async () =>
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                    await _connection.DisposeAsync();
                }

                _connection = await _connectionFactory.CreateConnectionAsync(_projectSetting.RabbitMq.ConnectionName);
                _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task OnConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
    {
        _logger.LogError("RabbitMQ connection shutdown. Reason: {Reason}", e.ReplyText);
        await HandleConnectionAsync();
    }
}
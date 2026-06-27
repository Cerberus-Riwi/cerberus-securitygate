using System.Text;
using System.Text.Json;
using cerberus_securitygate.Models;
using RabbitMQ.Client;

namespace cerberus_securitygate.Services;

public class ScanRequestPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string Exchange = "cerberus.scan.requests";

    private ScanRequestPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<ScanRequestPublisher> CreateAsync(string host, int port, string user, string password)
    {
        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = password
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false);

        return new ScanRequestPublisher(connection, channel);
    }

    public async Task PublishAsync(ScanRequest scanRequest)
    {
        var message = new
        {
            scanId = scanRequest.ScanId,
            repositoryUrl = scanRequest.RepositoryUrl,
            branch = scanRequest.Branch,
            commitHash = scanRequest.CommitHash,
            requestedAt = scanRequest.RequestedAt,
            metadata = new
            {
                prNumber = scanRequest.PrNumber,
                triggeredBy = scanRequest.TriggeredBy
            }
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = scanRequest.ScanId.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
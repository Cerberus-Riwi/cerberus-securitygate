using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;

namespace cerberus_securitygate.Services;

public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchange;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private RabbitMqPublisher(IConnection connection, IChannel channel, string exchange)
    {
        _connection = connection;
        _channel = channel;
        _exchange = exchange;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(string host, int port, string user, string password, string exchange)
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
            exchange: exchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false);

        return new RabbitMqPublisher(connection, channel, exchange);
    }

    public async Task PublishAsync(object message, string messageId)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, SerializerOptions));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = messageId,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: _exchange,
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

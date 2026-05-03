using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;
using System.Text.RegularExpressions;

namespace TraceIot.MqttWorker;

/// <summary>
/// MQTT 后台订阅服务：连接 EMQX，订阅所有设备 GPS 上报 Topic，断线自动重连
/// </summary>
public class GpsMqttWorker : BackgroundService
{
    private readonly GpsMessageHandler        _handler;
    private readonly IConfiguration           _config;
    private readonly ILogger<GpsMqttWorker>   _logger;

    // 从 topic 中提取 deviceId 的正则：gps/{deviceId}/location 或 gps/{deviceId}/heartbeat
    private static readonly Regex TopicRegex = new(@"^gps/([^/]+)/(location|heartbeat)$");

    public GpsMqttWorker(GpsMessageHandler handler, IConfiguration config, ILogger<GpsMqttWorker> logger)
    {
        _handler = handler;
        _config  = config;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndSubscribeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT 连接断开，5 秒后重连...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken stoppingToken)
    {
        var host     = _config["Mqtt:Host"]     ?? "localhost";
        var port     = int.Parse(_config["Mqtt:Port"] ?? "1883");
        var clientId = _config["Mqtt:ClientId"] ?? "traceiot-backend";
        var username = _config["Mqtt:Username"] ?? "";
        var password = _config["Mqtt:Password"] ?? "";

        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId($"{clientId}-{Guid.NewGuid():N}")
            .WithCleanSession(true)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60));

        if (!string.IsNullOrWhiteSpace(username))
            optionsBuilder.WithCredentials(username, password);

        var options = optionsBuilder.Build();

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic   = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            var match   = TopicRegex.Match(topic);

            if (!match.Success) return;

            var deviceId = match.Groups[1].Value;
            var msgType  = match.Groups[2].Value;

            if (msgType == "location")
                await _handler.HandleLocationAsync(topic, payload);
            else if (msgType == "heartbeat")
                await _handler.HandleHeartbeatAsync(deviceId);
        };

        client.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("MQTT 连接断开: {Reason}", e.ReasonString);
            await Task.CompletedTask;
        };

        _logger.LogInformation("正在连接 MQTT Broker: {Host}:{Port}", host, port);
        await client.ConnectAsync(options, stoppingToken);
        _logger.LogInformation("MQTT 连接成功，ClientId: {ClientId}", options.ClientId);

        // 订阅所有设备的 location 和 heartbeat
        await client.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic("gps/+/location")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build(), stoppingToken);

        await client.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic("gps/+/heartbeat")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build(), stoppingToken);

        _logger.LogInformation("MQTT 订阅成功：gps/+/location, gps/+/heartbeat");

        // 保持连接直到取消或断线
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

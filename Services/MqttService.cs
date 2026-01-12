using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using RNG.Models;

namespace RNG.Services;

public class MqttService
{
    private static MqttService? _instance;
    public static MqttService Instance => _instance ??= new MqttService();

    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private bool _isConnecting = false;

    // 🔥 EVENTLER
    public event Action<string, DeviceData>? OnDeviceDataReceived;
    public event Action<MqttConnectionState>? OnConnectionStateChanged;

    private MqttService()
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        // ✅ BAĞLANDI
        _client.ConnectedAsync += e =>
        {
            Console.WriteLine("✅ MQTT Connected");
            OnConnectionStateChanged?.Invoke(MqttConnectionState.Connected);
            return Task.CompletedTask;
        };

        // ✅ BAĞLANTI KOPTU
        _client.DisconnectedAsync += e =>
        {
            Console.WriteLine("❌ MQTT Disconnected");
            OnConnectionStateChanged?.Invoke(MqttConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        // ✅ MESAJ GELDİ
        _client.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                
                // 🔥 HATA 1 DÜZELTİLDİ: Payload yerine PayloadSegment kullanılmalı (MQTTnet 4.x)
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                Console.WriteLine($"📩 Mesaj geldi - Topic: {topic}");
                Console.WriteLine($"📦 Payload: {payload}");

                var data = JsonSerializer.Deserialize<DeviceData>(payload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data != null)
                    OnDeviceDataReceived?.Invoke(topic, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ MQTT JSON Hatası: " + ex.Message);
            }

            return Task.CompletedTask;
        };

        _options = new MqttClientOptionsBuilder()
            .WithClientId("MAUI_" + Guid.NewGuid())
            .WithTcpServer("broker.hivemq.com", 1883)
            .WithCleanSession()
            .Build();
    }

    public bool IsConnected => _client.IsConnected;

    // ✅ TEK VE GÜVENLİ BAĞLANTI
    public async Task ConnectAsync()
    {
        if (_client.IsConnected || _isConnecting)
            return;

        try
        {
            _isConnecting = true;
            OnConnectionStateChanged?.Invoke(MqttConnectionState.Connecting);

            await _client.ConnectAsync(_options);
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ MQTT Bağlantı Hatası: " + ex.Message);
            OnConnectionStateChanged?.Invoke(MqttConnectionState.Disconnected);
        }
        finally
        {
            _isConnecting = false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync();
        }
    }

    public async Task SubscribeAsync(string topic)
    {
        if (!_client.IsConnected)
            return;

        // 🔥 HATA 2 DÜZELTİLDİ: MqttTopicFilterBuilder yerine MqttFactory kullanılmalı (MQTTnet 4.x)
        var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(topic))
            .Build();

        await _client.SubscribeAsync(subscribeOptions);
        Console.WriteLine($"📡 Subscribed to: {topic}");
    }

    public async Task PublishAsync(string topic, string message)
    {
        if (!_client.IsConnected)
            return;

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message)
            .Build();

        await _client.PublishAsync(mqttMessage);
        Console.WriteLine($"📤 Published to {topic}: {message}");
    }

    public async Task PublishAsync(string topic, DeviceData data)
    {
        var json = JsonSerializer.Serialize(data);
        await PublishAsync(topic, json);
    }
}

public enum MqttConnectionState
{
    Disconnected,
    Connecting,
    Connected
}

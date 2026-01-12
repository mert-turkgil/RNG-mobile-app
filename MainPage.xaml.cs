using RNG.Services;
using RNG.Models;

namespace RNG;

public partial class MainPage : ContentPage
{
    private readonly MqttService _mqtt;
    private List<string> _messages = new();

    public MainPage()
    {
        InitializeComponent();
        _mqtt = MqttService.Instance;

        // Event'lere abone ol
        _mqtt.OnConnectionStateChanged += OnConnectionStateChanged;
        _mqtt.OnDeviceDataReceived += OnDeviceDataReceived;
    }

    private void OnConnectionStateChanged(MqttConnectionState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (state)
            {
                case MqttConnectionState.Connected:
                    ConnectionStatusLabel.Text = "✅ Bağlandı";
                    ConnectionStatusLabel.TextColor = Colors.LightGreen;
                    ConnectBtn.IsEnabled = false;
                    DisconnectBtn.IsEnabled = true;
                    break;

                case MqttConnectionState.Connecting:
                    ConnectionStatusLabel.Text = "🔄 Bağlanıyor...";
                    ConnectionStatusLabel.TextColor = Colors.Yellow;
                    ConnectBtn.IsEnabled = false;
                    DisconnectBtn.IsEnabled = false;
                    break;

                case MqttConnectionState.Disconnected:
                    ConnectionStatusLabel.Text = "❌ Bağlı Değil";
                    ConnectionStatusLabel.TextColor = Color.FromArgb("#FF6B6B");
                    ConnectBtn.IsEnabled = true;
                    DisconnectBtn.IsEnabled = false;
                    break;
            }
        });
    }

    private void OnDeviceDataReceived(string topic, DeviceData data)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var msg = $"[{DateTime.Now:HH:mm:ss}] {topic}\n" +
                      $"  Device: {data.DeviceId}\n" +
                      $"  Temp: {data.Temperature}°C, Humidity: {data.Humidity}%\n" +
                      $"  Status: {data.Status}\n";

            _messages.Insert(0, msg);
            if (_messages.Count > 20) _messages.RemoveAt(_messages.Count - 1);

            MessagesLabel.Text = string.Join("\n", _messages);
            MessagesLabel.TextColor = Colors.LightGreen;
        });
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        await _mqtt.ConnectAsync();
    }

    private async void OnDisconnectClicked(object? sender, EventArgs e)
    {
        await _mqtt.DisconnectAsync();
    }

    private async void OnSubscribeClicked(object? sender, EventArgs e)
    {
        var topic = SubscribeTopicEntry.Text?.Trim();
        if (string.IsNullOrEmpty(topic))
        {
            await DisplayAlert("Hata", "Topic giriniz", "Tamam");
            return;
        }

        if (!_mqtt.IsConnected)
        {
            await DisplayAlert("Hata", "Önce bağlanmalısınız", "Tamam");
            return;
        }

        await _mqtt.SubscribeAsync(topic);
        await DisplayAlert("Başarılı", $"'{topic}' topic'ine abone olundu", "Tamam");
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        var topic = PublishTopicEntry.Text?.Trim();
        var message = MessageEntry.Text?.Trim();

        if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(message))
        {
            await DisplayAlert("Hata", "Topic ve mesaj giriniz", "Tamam");
            return;
        }

        if (!_mqtt.IsConnected)
        {
            await DisplayAlert("Hata", "Önce bağlanmalısınız", "Tamam");
            return;
        }

        await _mqtt.PublishAsync(topic, message);
        MessageEntry.Text = "";
    }

    private async void OnPublishTestDataClicked(object? sender, EventArgs e)
    {
        var topic = PublishTopicEntry.Text?.Trim();

        if (string.IsNullOrEmpty(topic))
        {
            await DisplayAlert("Hata", "Topic giriniz", "Tamam");
            return;
        }

        if (!_mqtt.IsConnected)
        {
            await DisplayAlert("Hata", "Önce bağlanmalısınız", "Tamam");
            return;
        }

        var testData = new DeviceData
        {
            DeviceId = "RNG_Device_001",
            Temperature = Random.Shared.Next(15, 35) + Random.Shared.NextDouble(),
            Humidity = Random.Shared.Next(30, 80) + Random.Shared.NextDouble(),
            Status = "Active",
            Timestamp = DateTime.Now
        };

        await _mqtt.PublishAsync(topic, testData);
    }

    private void OnClearMessagesClicked(object? sender, EventArgs e)
    {
        _messages.Clear();
        MessagesLabel.Text = "Henüz mesaj yok...";
        MessagesLabel.TextColor = Color.FromArgb("#888888");
    }
}

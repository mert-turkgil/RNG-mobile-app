namespace RNG.Models;

public class DeviceData
{
    public string? DeviceId { get; set; }
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public string? Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

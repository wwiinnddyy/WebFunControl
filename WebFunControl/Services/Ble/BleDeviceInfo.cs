namespace WebFunControl.Services.Ble;

/// <summary>
/// BLE设备信息
/// </summary>
public class BleDeviceInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Rssi { get; init; }
}

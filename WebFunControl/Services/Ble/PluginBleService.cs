using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using WebFunControl.Models;

namespace WebFunControl.Services.Ble;

/// <summary>
/// 基于Plugin.BLE的统一跨平台BLE服务实现
/// 严格对齐HTML中Web Bluetooth API的行为
/// </summary>
public class PluginBleService : IBleService
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private readonly Dictionary<string, IService> _serviceCache = new();
    private readonly Dictionary<string, ICharacteristic> _charCache = new();
    private readonly List<(string ServiceUuid, string CharUuid, Action<byte[]> Callback)> _subscriptions = new();
    private bool _disposed;

    public bool IsConnected => _connectedDevice != null && _adapter.ConnectedDevices.Count > 0;

    public event EventHandler<bool>? ConnectionStateChanged;

    public PluginBleService()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
    }

    public async Task<BleDeviceInfo> ScanAndConnectAsync(string deviceName, CancellationToken ct = default)
    {
        // 检查蓝牙可用性
        if (!_ble.IsAvailable)
            throw new InvalidOperationException("蓝牙不可用，请确认设备支持BLE并已开启蓝牙");

        if (!_ble.IsOn)
            throw new InvalidOperationException("蓝牙未开启，请先开启蓝牙");

        // 清理旧连接
        if (_connectedDevice != null)
            await DisconnectAsync();

        // 扫描设备 — 对应HTML: navigator.bluetooth.requestDevice({ filters: [{ name: "W66D" }] })
        var tcs = new TaskCompletionSource<IDevice>();

        _adapter.ScanTimeout = 15000;
        _adapter.ScanMode = ScanMode.LowLatency;

        void OnDeviceDiscovered(object? sender, DeviceEventArgs args)
        {
            if (args.Device.Name?.Equals(deviceName, StringComparison.OrdinalIgnoreCase) == true
                && !tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(args.Device);
            }
        }

        _adapter.DeviceDiscovered += OnDeviceDiscovered;

        try
        {
            // 开始扫描（异步，持续扫描直到超时或手动停止）
            await _adapter.StartScanningForDevicesAsync(
                deviceFilter: d => d.Name?.Equals(deviceName, StringComparison.OrdinalIgnoreCase) == true);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            cts.Token.Register(() => tcs.TrySetCanceled());

            var foundDevice = await tcs.Task;

            // 连接设备 — 对应HTML: device.gatt.connect()
            var connectArgs = new ConnectParameters(forceBleTransport: true);
            await _adapter.ConnectToDeviceAsync(foundDevice, connectArgs);

            _connectedDevice = foundDevice;

            // 预缓存服务和特征 — 对应HTML中获取 mainService/ffd0Service/natureWindService
            await CacheServicesAndCharacteristicsAsync();

            ConnectionStateChanged?.Invoke(this, true);

            return new BleDeviceInfo
            {
                Id = foundDevice.Id.ToString(),
                Name = foundDevice.Name ?? deviceName,
                Rssi = foundDevice.Rssi
            };
        }
        finally
        {
            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
            if (_adapter.IsScanning)
                await _adapter.StopScanningForDevicesAsync();
        }
    }

    private async Task CacheServicesAndCharacteristicsAsync()
    {
        if (_connectedDevice == null) return;

        var services = await _connectedDevice.GetServicesAsync();

        foreach (var service in services)
        {
            var serviceKey = service.Id.ToString().ToLowerInvariant();
            _serviceCache[serviceKey] = service;

            var characteristics = await service.GetCharacteristicsAsync();
            foreach (var characteristic in characteristics)
            {
                var charKey = $"{serviceKey}_{characteristic.Id.ToString().ToLowerInvariant()}";
                _charCache[charKey] = characteristic;
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connectedDevice == null) return;

        try
        {
            foreach (var sub in _subscriptions.ToList())
            {
                try { await UnsubscribeNotificationAsync(sub.ServiceUuid, sub.CharUuid); }
                catch { }
            }
            _subscriptions.Clear();

            await _adapter.DisconnectDeviceAsync(_connectedDevice);
        }
        finally
        {
            _serviceCache.Clear();
            _charCache.Clear();
            _connectedDevice = null;
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    public async Task WriteCharacteristicAsync(string serviceUuid, string charUuid, byte[] data, bool withoutResponse = true, CancellationToken ct = default)
    {
        var characteristic = await GetCharacteristicAsync(serviceUuid, charUuid);

        if (characteristic.CanWrite)
        {
            await characteristic.WriteAsync(data, ct);
        }
        else
        {
            throw new InvalidOperationException($"特征 {charUuid} 不支持写入");
        }
    }

    public async Task<byte[]> ReadCharacteristicAsync(string serviceUuid, string charUuid, CancellationToken ct = default)
    {
        var characteristic = await GetCharacteristicAsync(serviceUuid, charUuid);

        if (!characteristic.CanRead)
            throw new InvalidOperationException($"特征 {charUuid} 不支持读取");

        var (data, resultCode) = await characteristic.ReadAsync(ct);
        return data ?? [];
    }

    public async Task SubscribeNotificationAsync(string serviceUuid, string charUuid, Action<byte[]> callback, CancellationToken ct = default)
    {
        var characteristic = await GetCharacteristicAsync(serviceUuid, charUuid);

        if (!characteristic.CanUpdate)
            throw new InvalidOperationException($"特征 {charUuid} 不支持通知");

        characteristic.ValueUpdated += (sender, args) =>
        {
            callback(args.Characteristic.Value ?? []);
        };

        await characteristic.StartUpdatesAsync(ct);
        _subscriptions.Add((serviceUuid, charUuid, callback));
    }

    public async Task UnsubscribeNotificationAsync(string serviceUuid, string charUuid, CancellationToken ct = default)
    {
        var characteristic = await GetCharacteristicAsync(serviceUuid, charUuid);

        if (characteristic.CanUpdate)
            await characteristic.StopUpdatesAsync(ct);

        _subscriptions.RemoveAll(s => s.ServiceUuid == serviceUuid && s.CharUuid == charUuid);
    }

    private async Task<ICharacteristic> GetCharacteristicAsync(string serviceUuid, string charUuid)
    {
        if (_connectedDevice == null)
            throw new InvalidOperationException("设备未连接，请先连接风扇");

        var serviceKey = Guid.Parse(serviceUuid).ToString().ToLowerInvariant();
        var charKey = $"{serviceKey}_{Guid.Parse(charUuid).ToString().ToLowerInvariant()}";

        if (_charCache.TryGetValue(charKey, out var cached))
            return cached;

        if (!_serviceCache.TryGetValue(serviceKey, out var service))
        {
            var serviceGuid = Guid.Parse(serviceUuid);
            service = await _connectedDevice.GetServiceAsync(serviceGuid)
                ?? throw new InvalidOperationException($"未找到服务: {serviceUuid}");
            _serviceCache[serviceKey] = service;
        }

        var charGuid = Guid.Parse(charUuid);
        var characteristic = await service.GetCharacteristicAsync(charGuid)
            ?? throw new InvalidOperationException($"未找到特征: {charUuid}");

        _charCache[charKey] = characteristic;
        return characteristic;
    }

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        if (e.Device == _connectedDevice)
        {
            _connectedDevice = null;
            _serviceCache.Clear();
            _charCache.Clear();
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    private void OnDeviceConnectionLost(object? sender, DeviceErrorEventArgs e)
    {
        if (e.Device == _connectedDevice)
        {
            _connectedDevice = null;
            _serviceCache.Clear();
            _charCache.Clear();
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisconnectAsync();
        _adapter.DeviceDisconnected -= OnDeviceDisconnected;
        _adapter.DeviceConnectionLost -= OnDeviceConnectionLost;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { DisconnectAsync().GetAwaiter().GetResult(); } catch { }
        _adapter.DeviceDisconnected -= OnDeviceDisconnected;
        _adapter.DeviceConnectionLost -= OnDeviceConnectionLost;
    }
}

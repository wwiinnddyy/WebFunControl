namespace WebFunControl.Services.Ble;

/// <summary>
/// BLE服务抽象接口 — 从HTML的Web Bluetooth API抽取
/// 屏蔽各平台BLE实现差异，提供统一的读写接口
/// </summary>
public interface IBleService : IAsyncDisposable, IDisposable
{
    /// <summary>当前是否已连接</summary>
    bool IsConnected { get; }

    /// <summary>连接状态变化事件</summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// 扫描并连接指定名称的BLE设备
    /// 对应HTML: navigator.bluetooth.requestDevice({ filters: [{ name: "W66D" }] })
    /// </summary>
    Task<BleDeviceInfo> ScanAndConnectAsync(string deviceName, CancellationToken ct = default);

    /// <summary>
    /// 断开连接
    /// 对应HTML: device.gatt.disconnect()
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 向指定特征写入数据（优先writeWithoutResponse，备选write）
    /// 对应HTML: safeWrite(characteristicKey, hexValue)
    /// </summary>
    Task WriteCharacteristicAsync(string serviceUuid, string charUuid, byte[] data, bool withoutResponse = true, CancellationToken ct = default);

    /// <summary>
    /// 读取指定特征的值
    /// 对应HTML: characteristic.readValue()
    /// </summary>
    Task<byte[]> ReadCharacteristicAsync(string serviceUuid, string charUuid, CancellationToken ct = default);

    /// <summary>
    /// 订阅特征通知（预留接口，HTML中未使用）
    /// </summary>
    Task SubscribeNotificationAsync(string serviceUuid, string charUuid, Action<byte[]> callback, CancellationToken ct = default);

    /// <summary>
    /// 取消订阅特征通知
    /// </summary>
    Task UnsubscribeNotificationAsync(string serviceUuid, string charUuid, CancellationToken ct = default);
}

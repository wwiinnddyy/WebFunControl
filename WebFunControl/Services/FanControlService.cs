using System.Buffers.Binary;
using WebFunControl.Models;
using WebFunControl.Services.Ble;

namespace WebFunControl.Services;

/// <summary>
/// 风扇控制业务逻辑层 — 将HTML中的所有功能函数抽取为结构化服务
/// 封装BLE通信细节，提供类型安全的风扇控制接口
/// </summary>
public class FanControlService
{
    private readonly IBleService _bleService;

    public FanControlService(IBleService bleService)
    {
        _bleService = bleService;
        _bleService.ConnectionStateChanged += (_, isConnected) => ConnectionStateChanged?.Invoke(this, isConnected);
    }

    /// <summary>是否已连接</summary>
    public bool IsConnected => _bleService.IsConnected;

    /// <summary>连接状态变化事件</summary>
    public event EventHandler<bool>? ConnectionStateChanged;

    // ==================== 连接管理 ====================

    /// <summary>
    /// 扫描并连接W66D风扇
    /// 对应HTML: connect()
    /// </summary>
    public async Task<BleDeviceInfo> ConnectAsync(CancellationToken ct = default)
    {
        return await _bleService.ScanAndConnectAsync(FanConstants.DeviceName, ct);
    }

    /// <summary>
    /// 断开连接
    /// 对应HTML: disconnect()
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _bleService.DisconnectAsync();
    }

    // ==================== 档位控制 ====================

    /// <summary>
    /// 设置电源档位
    /// 对应HTML: safeWrite('POWER', '00'~'04')
    /// </summary>
    public async Task SetPowerAsync(PowerMode mode, CancellationToken ct = default)
    {
        await _bleService.WriteCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.PowerCharUuid,
            [(byte)mode], ct: ct);
    }

    // ==================== 转速控制 ====================

    /// <summary>
    /// 设置风扇转速百分比 (0-100)
    /// 对应HTML: safeWrite('FAN_SPEED', hexValue)
    /// </summary>
    public async Task SetFanSpeedAsync(int percentage, CancellationToken ct = default)
    {
        percentage = Math.Clamp(percentage, 0, 100);
        await _bleService.WriteCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.FanSpeedCharUuid,
            [(byte)percentage], ct: ct);
    }

    /// <summary>
    /// 读取当前风扇转速百分比
    /// 对应HTML: speedChar.readValue() → getUint8(0)
    /// </summary>
    public async Task<int> ReadFanSpeedAsync(CancellationToken ct = default)
    {
        var data = await _bleService.ReadCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.FanSpeedCharUuid, ct);
        return data.Length > 0 ? data[0] : 0;
    }

    // ==================== 定时控制 ====================

    /// <summary>
    /// 设置定时（秒）
    /// 对应HTML: safeWrite('TIMER', hexValue) — 2字节大端
    /// </summary>
    public async Task SetTimerAsync(int seconds, CancellationToken ct = default)
    {
        var data = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(data, (ushort)Math.Clamp(seconds, 0, 65535));
        await _bleService.WriteCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.TimerCharUuid,
            data, ct: ct);
    }

    /// <summary>
    /// 设置定时（分钟）
    /// 对应HTML: setCustomTimer()
    /// </summary>
    public async Task SetTimerMinutesAsync(int minutes, CancellationToken ct = default)
    {
        await SetTimerAsync(minutes * 60, ct);
    }

    /// <summary>
    /// 取消定时
    /// 对应HTML: cancelTimer() → safeWrite('TIMER', '0000')
    /// </summary>
    public async Task CancelTimerAsync(CancellationToken ct = default)
    {
        await SetTimerAsync(0, ct);
    }

    /// <summary>
    /// 读取剩余定时时间
    /// 对应HTML: readRemainingTime()
    /// </summary>
    public async Task<TimeSpan?> ReadRemainingTimeAsync(CancellationToken ct = default)
    {
        var data = await _bleService.ReadCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.TimerCharUuid, ct);

        if (data.Length < 2) return null;

        var seconds = BinaryPrimitives.ReadUInt16BigEndian(data);
        return seconds > 0 ? TimeSpan.FromSeconds(seconds) : null;
    }

    // ==================== 自然风控制 ====================

    /// <summary>
    /// 设置自然风开关
    /// 对应HTML: toggleNW() → safeWrite('NATURE_WIND', '01'/'00')
    /// </summary>
    public async Task SetNatureWindAsync(bool enabled, CancellationToken ct = default)
    {
        await _bleService.WriteCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.NatureWindCharUuid,
            [(byte)(enabled ? 0x01 : 0x00)], ct: ct);
    }

    /// <summary>
    /// 写入自然风曲线数据（128字节，每字节20-90）
    /// 对应HTML: applyEditedCurve()
    /// </summary>
    public async Task WriteNatureWindCurveAsync(byte[] curveData, CancellationToken ct = default)
    {
        if (curveData.Length != FanConstants.CurveDataPoints)
            throw new ArgumentException($"曲线数据必须包含{FanConstants.CurveDataPoints}个字节");

        // 验证数据范围
        for (int i = 0; i < curveData.Length; i++)
        {
            if (curveData[i] < FanConstants.MinSpeedLimit || curveData[i] > FanConstants.MaxSpeedLimit)
                throw new ArgumentOutOfRangeException(nameof(curveData),
                    $"第{i + 1}个值{curveData[i]}超出范围({FanConstants.MinSpeedLimit}-{FanConstants.MaxSpeedLimit})");
        }

        await _bleService.WriteCharacteristicAsync(
            FanConstants.NatureWindServiceUuid,
            FanConstants.NatureWindCurveCharUuid,
            curveData, withoutResponse: false, ct: ct);
    }

    /// <summary>
    /// 读取自然风曲线数据
    /// 对应HTML: readNatureWindCurve()
    /// </summary>
    public async Task<byte[]> ReadNatureWindCurveAsync(CancellationToken ct = default)
    {
        var data = await _bleService.ReadCharacteristicAsync(
            FanConstants.NatureWindServiceUuid,
            FanConstants.NatureWindCurveCharUuid, ct);

        // 确保数据在有效范围内
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] < FanConstants.MinSpeedLimit) data[i] = FanConstants.MinSpeedLimit;
            if (data[i] > FanConstants.MaxSpeedLimit) data[i] = FanConstants.MaxSpeedLimit;
        }

        return data;
    }

    // ==================== 档位风速标定 ====================

    /// <summary>
    /// 设置4档风速标定
    /// 对应HTML: setCustomSpeeds() → safeWrite('SPEED_CALIB', hexValues)
    /// </summary>
    public async Task SetGearSpeedsAsync(int[] speeds, CancellationToken ct = default)
    {
        if (speeds.Length != 4)
            throw new ArgumentException("必须提供4个档位的风速值");

        for (int i = 0; i < 4; i++)
        {
            if (speeds[i] < FanConstants.MinSpeedLimit || speeds[i] > 100)
                throw new ArgumentOutOfRangeException(nameof(speeds),
                    $"档位{i + 1}的风速必须在{FanConstants.MinSpeedLimit}-100之间");
        }

        var data = speeds.Select(s => (byte)s).ToArray();
        await _bleService.WriteCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.SpeedCalibCharUuid,
            data, ct: ct);
    }

    /// <summary>
    /// 读取当前档位风速标定
    /// 对应HTML: readCurrentSpeeds()
    /// </summary>
    public async Task<int[]> ReadGearSpeedsAsync(CancellationToken ct = default)
    {
        var data = await _bleService.ReadCharacteristicAsync(
            FanConstants.MainServiceUuid,
            FanConstants.SpeedCalibCharUuid, ct);

        var speeds = new int[4];
        for (int i = 0; i < 4 && i < data.Length; i++)
            speeds[i] = data[i];

        // 不足4个用默认值补齐
        for (int i = data.Length; i < 4; i++)
            speeds[i] = FanConstants.DefaultGearSpeeds[i];

        return speeds;
    }

    // ==================== 实时状态读取 ====================

    /// <summary>
    /// 读取完整的风扇实时状态
    /// 对应HTML: refreshRealTimeStatus()
    /// </summary>
    public async Task<FanStatus> ReadRealTimeStatusAsync(CancellationToken ct = default)
    {
        var status = new FanStatus();

        // 读取风扇转速
        try { status.FanSpeed = await ReadFanSpeedAsync(ct); }
        catch { status.FanSpeed = -1; }

        // 读取系统电压（大端，offset 2-3）
        try
        {
            var voltageData = await _bleService.ReadCharacteristicAsync(
                FanConstants.Ffd0ServiceUuid, FanConstants.VoltageCharUuid, ct);
            if (voltageData.Length >= 4)
            {
                var raw = BinaryPrimitives.ReadUInt16BigEndian(voltageData.AsSpan(2, 2));
                status.Voltage = raw / 1000.0;
            }
        }
        catch { status.Voltage = -1; }

        // 读取电池电压（大端，offset 0-1）
        try
        {
            var batteryData = await _bleService.ReadCharacteristicAsync(
                FanConstants.Ffd0ServiceUuid, FanConstants.BatteryVoltageCharUuid, ct);
            if (batteryData.Length >= 2)
            {
                var raw = BinaryPrimitives.ReadUInt16BigEndian(batteryData.AsSpan(0, 2));
                status.BatteryVoltage = raw / 1000.0;
            }
        }
        catch { status.BatteryVoltage = -1; }

        // 读取电流（大端，offset 0-1，×0.63÷1000）
        try
        {
            var currentData = await _bleService.ReadCharacteristicAsync(
                FanConstants.Ffd0ServiceUuid, FanConstants.CurrentCharUuid, ct);
            if (currentData.Length >= 2)
            {
                var raw = BinaryPrimitives.ReadUInt16BigEndian(currentData.AsSpan(0, 2));
                status.Current = raw * FanConstants.CurrentCalibrationFactor / 1000.0;
            }
        }
        catch { status.Current = -1; }

        // 计算功率
        if (status.BatteryVoltage > 0 && status.Current > 0)
            status.Power = status.BatteryVoltage * status.Current;

        return status;
    }

    // ==================== 自然风曲线生成算法 ====================

    /// <summary>
    /// 生成随机自然风曲线
    /// 对应HTML: generateRandomCurve()
    /// </summary>
    public byte[] GenerateRandomCurve(NatureWindCurveMode mode = NatureWindCurveMode.Smooth)
    {
        int baseMin = FanConstants.MinSpeedLimit;
        int baseMax = FanConstants.MaxSpeedLimit;
        int baseValue = 55;
        int variationRange = 25;
        double smoothingStrength = 0.3;

        if (mode.HasFlag(NatureWindCurveMode.Quiet))
        {
            baseMin = 20; baseMax = 50; baseValue = 30; variationRange = 15;
        }
        if (mode.HasFlag(NatureWindCurveMode.Strong))
        {
            baseMin = 50; baseMax = 90; baseValue = 70; variationRange = 20;
        }
        if (mode.HasFlag(NatureWindCurveMode.Smooth))
        {
            smoothingStrength = 0.6;
        }

        var random = Random.Shared;
        var data = new double[128];

        // 随机选择策略
        var strategy = random.Next(4);

        for (int i = 0; i < 128; i++)
        {
            double value = strategy switch
            {
                0 => GenerateWavePattern(i, baseValue, variationRange, random),
                1 => GenerateRandomWalk(i, data, baseValue, variationRange, random),
                2 => GenerateGustPattern(i, baseMin, baseMax, baseValue, variationRange, mode, random),
                _ => GenerateSteppedPattern(i, data, baseMin, baseMax, baseValue, variationRange, random)
            };

            data[i] = Math.Clamp(Math.Round(value), baseMin, baseMax);
        }

        // 应用平滑
        if (mode.HasFlag(NatureWindCurveMode.Smooth))
            data = ApplySmoothing(data, smoothingStrength);

        return data.Select(d => (byte)d).ToArray();
    }

    private static double GenerateWavePattern(int index, double baseValue, int range, Random rng)
    {
        double freq1 = 0.03 + rng.NextDouble() * 0.05;
        double freq2 = 0.08 + rng.NextDouble() * 0.1;
        double freq3 = 0.15 + rng.NextDouble() * 0.2;

        double value = baseValue;
        value += Math.Sin(index * freq1) * range * 0.5;
        value += Math.Sin(index * freq2) * range * 0.3;
        value += Math.Sin(index * freq3) * range * 0.2;
        value += (rng.NextDouble() - 0.5) * range * 0.3;

        return value;
    }

    private static double GenerateRandomWalk(int index, double[] data, double baseValue, int range, Random rng)
    {
        if (index == 0)
            return baseValue + (rng.NextDouble() - 0.5) * range;

        return data[index - 1] + (rng.NextDouble() - 0.5) * range * 0.5;
    }

    private static double GenerateGustPattern(int index, int baseMin, int baseMax, double baseValue, int range, NatureWindCurveMode mode, Random rng)
    {
        double gustProb = mode.HasFlag(NatureWindCurveMode.Strong) ? 0.15 : 0.08;
        if (rng.NextDouble() < gustProb)
            return baseMax - rng.NextDouble() * range * 0.3;
        else
            return baseMin + rng.NextDouble() * range * 0.7;
    }

    private static double GenerateSteppedPattern(int index, double[] data, int baseMin, int baseMax, double baseValue, int range, Random rng)
    {
        int stepSize = 15 + rng.Next(11);
        if (index % stepSize == 0)
            return baseMin + rng.NextDouble() * (baseMax - baseMin);
        else
            return (index > 0 ? data[index - 1] : baseValue) + (rng.NextDouble() - 0.5) * range * 0.2;
    }

    private static double[] ApplySmoothing(double[] data, double strength)
    {
        var result = new double[data.Length];
        Array.Copy(data, result, data.Length);

        for (int i = 1; i < data.Length - 1; i++)
        {
            double weighted = (data[i - 1] * strength + data[i] + data[i + 1] * strength)
                           / (1 + 2 * strength);
            result[i] = Math.Round(weighted);
        }

        return result;
    }
}

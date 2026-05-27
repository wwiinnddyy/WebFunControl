namespace WebFunControl.Models;

/// <summary>
/// W66D风扇BLE通信常量配置（从HTML文件抽取）
/// </summary>
public static class FanConstants
{
    // ===== 服务UUID =====
    public const string MainServiceUuid = "0000fff0-0000-1000-8000-00805f9b34fb";
    public const string Ffd0ServiceUuid = "0000ffd0-0000-1000-8000-00805f9b34fb";
    public const string NatureWindServiceUuid = "0000ffe0-0000-1000-8000-00805f9b34fb";

    // ===== 特征UUID =====
    public const string PowerCharUuid = "0000fff1-0000-1000-8000-00805f9b34fb";
    public const string TimerCharUuid = "0000fff2-0000-1000-8000-00805f9b34fb";
    public const string NatureWindCharUuid = "0000fff4-0000-1000-8000-00805f9b34fb";
    public const string NatureWindCurveCharUuid = "0000ffe3-0000-1000-8000-00805f9b34fb";
    public const string SpeedCalibCharUuid = "0000fff7-0000-1000-8000-00805f9b34fb";
    public const string FanSpeedCharUuid = "0000fff3-0000-1000-8000-00805f9b34fb";
    public const string VoltageCharUuid = "0000ffd2-0000-1000-8000-00805f9b34fb";
    public const string BatteryVoltageCharUuid = "0000ffd1-0000-1000-8000-00805f9b34fb";
    public const string CurrentCharUuid = "0000ffd3-0000-1000-8000-00805f9b34fb";

    // ===== 设备名称 =====
    public const string DeviceName = "W66D";

    // ===== 风速范围 =====
    public const int MinSpeedLimit = 20;
    public const int MaxSpeedLimit = 90;
    public const int CurveDataPoints = 128;

    // ===== 默认值 =====
    public static readonly int[] DefaultGearSpeeds = [30, 50, 70, 100];
    public const int DefaultFanSpeed = 50;

    // ===== 电流校准系数 =====
    public const double CurrentCalibrationFactor = 0.63;

    // ===== 预设定时值 =====
    public const int OneHourSeconds = 3600;
    public const int FourHoursSeconds = 14400;

    // ===== 默认自然风曲线（128个数据点，范围20-90） =====
    public static readonly byte[] DefaultNatureWindCurve =
    [
        55, 48, 40, 33, 28, 22, 21, 26, 33, 41, 48, 54, 58, 60, 61, 58,
        52, 45, 37, 30, 24, 20, 25, 33, 40, 48, 53, 57, 60, 60, 56, 51,
        43, 36, 29, 23, 21, 28, 37, 47, 56, 63, 68, 71, 72, 71, 67, 62,
        54, 46, 36, 29, 23, 20, 27, 37, 48, 57, 64, 69, 73, 74, 76, 78,
        80, 82, 84, 86, 88, 90, 89, 87, 83, 77, 70, 62, 53, 43, 34, 27,
        21, 20, 26, 32, 38, 43, 47, 49, 50, 48, 44, 38, 33, 27, 24, 20,
        21, 26, 31, 37, 42, 46, 48, 47, 42, 36, 31, 27, 23, 20, 22, 27,
        33, 39, 44, 47, 48, 46, 41, 36, 30, 26, 23, 20, 22, 27, 33, 38
    ];
}

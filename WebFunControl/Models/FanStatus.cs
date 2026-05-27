namespace WebFunControl.Models;

/// <summary>
/// 风扇实时状态模型
/// </summary>
public class FanStatus
{
    /// <summary>风扇转速百分比 (0-100)</summary>
    public int FanSpeed { get; set; }

    /// <summary>系统电压 (V)</summary>
    public double Voltage { get; set; }

    /// <summary>电池电压 (V)</summary>
    public double BatteryVoltage { get; set; }

    /// <summary>电流 (A)，经0.63系数校准</summary>
    public double Current { get; set; }

    /// <summary>功率 (W) = 电池电压 × 电流</summary>
    public double Power { get; set; }

    /// <summary>剩余定时时间</summary>
    public TimeSpan? RemainingTime { get; set; }

    /// <summary>自然风开关状态</summary>
    public bool NatureWindEnabled { get; set; }

    /// <summary>4档风速标定值</summary>
    public int[] GearSpeeds { get; set; } = [30, 50, 70, 100];

    /// <summary>自然风曲线数据 (128字节)</summary>
    public byte[] NatureWindCurve { get; set; } = new byte[128];
}

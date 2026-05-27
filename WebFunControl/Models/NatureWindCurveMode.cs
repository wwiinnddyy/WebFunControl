namespace WebFunControl.Models;

/// <summary>
/// 自然风曲线生成模式
/// </summary>
[Flags]
public enum NatureWindCurveMode
{
    None = 0,
    Smooth = 1,
    Quiet = 2,
    Strong = 4
}

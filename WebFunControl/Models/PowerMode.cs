namespace WebFunControl.Models;

/// <summary>
/// 风扇电源档位模式
/// </summary>
public enum PowerMode : byte
{
    Off = 0x00,
    Gear1 = 0x01,
    Gear2 = 0x02,
    Gear3 = 0x03,
    Gear4 = 0x04
}

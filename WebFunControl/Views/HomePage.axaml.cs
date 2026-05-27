using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WebFunControl.Views;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        UpdateDeviceIndicators();
    }

    /// <summary>
    /// 更新 ComboBox 中设备状态指示灯颜色
    /// 已连接=绿色，在线=橙色，离线=灰色
    /// </summary>
    private void UpdateDeviceIndicators()
    {
        // ComboBox 中状态指示灯通过 ItemTemplate 绑定
        // 这里不需要额外处理，颜色在 XAML 中通过 DataTrigger 控制
    }
}

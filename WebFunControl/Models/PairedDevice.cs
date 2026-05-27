using System.ComponentModel;
using Avalonia.Media;

namespace WebFunControl.Models;

/// <summary>
/// 已配对设备模型 — 记住用户连接过的设备
/// </summary>
public class PairedDevice : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    /// <summary>设备ID（蓝牙地址）</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>设备名称</summary>
    public string Name { get; set; } = string.Empty;

    private bool _isOnline;
    /// <summary>是否当前在线（运行时状态，不持久化）</summary>
    public bool IsOnline { get => _isOnline; set => SetProperty(ref _isOnline, value); }

    private bool _isConnected;
    /// <summary>是否当前已连接（运行时状态，不持久化）</summary>
    public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }

    /// <summary>显示名称</summary>
    public string DisplayName => string.IsNullOrEmpty(Name) ? Id : Name;

    /// <summary>状态指示灯颜色：已连接=绿色，在线=橙色，离线=灰色</summary>
    public ISolidColorBrush StatusBrush => IsConnected
        ? new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94))
        : IsOnline
            ? new SolidColorBrush(Color.FromRgb(0xFD, 0xCB, 0x6E))
            : new SolidColorBrush(Color.FromRgb(0xAD, 0xB5, 0xBD));

    /// <summary>显示状态文字</summary>
    public string StatusText => IsConnected ? "已连接" : IsOnline ? "在线" : "离线";

    /// <summary>文字透明度：离线时置灰</summary>
    public double TextOpacity => IsConnected ? 1.0 : IsOnline ? 0.8 : 0.4;

    /// <summary>通知属性变化</summary>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsConnected) || e.PropertyName == nameof(IsOnline))
        {
            OnPropertyChanged(nameof(StatusBrush));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(TextOpacity));
        }
    }
}

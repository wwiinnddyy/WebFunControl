using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebFunControl.Models;
using WebFunControl.Services;

namespace WebFunControl.ViewModels;

/// <summary>
/// 主ViewModel — 聚合所有子功能模块
/// 全面使用 FluentAvalonia 控件绑定
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly FanControlService _fanService;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatusText = "未连接";

    [ObservableProperty]
    private string _deviceInfo = string.Empty;

    // ===== 关于信息 =====
    [ObservableProperty]
    private string _version = Models.AppVersion.Current;

    [ObservableProperty]
    private string _applicationName = Models.AppVersion.AppName;

    [ObservableProperty]
    private string _applicationDescription = Models.AppVersion.Description;

    [ObservableProperty]
    private string _applicationProjectUrl = Models.AppVersion.ProjectUrl;

    // ===== 实时状态 =====
    [ObservableProperty]
    private string _fanSpeedText = "-- %";

    [ObservableProperty]
    private string _voltageText = "-- V";

    [ObservableProperty]
    private string _batteryVoltageText = "-- V";

    [ObservableProperty]
    private string _currentText = "-- A";

    [ObservableProperty]
    private string _powerText = "-- W";

    [ObservableProperty]
    private string _remainingTimeText = "未设置";

    // ===== 转速控制 =====
    [ObservableProperty]
    private int _fanSpeed = FanConstants.DefaultFanSpeed;

    [ObservableProperty]
    private int _manualSpeedInput = FanConstants.DefaultFanSpeed;

    // ===== 定时控制 =====
    [ObservableProperty]
    private int _timerMinutes = 30;

    // ===== 自然风控制 =====
    [ObservableProperty]
    private bool _natureWindEnabled;

    [ObservableProperty]
    private string _natureWindButtonText = "自然风 OFF";

    [ObservableProperty]
    private bool _smoothMode = true;

    [ObservableProperty]
    private bool _quietMode;

    [ObservableProperty]
    private bool _strongMode;

    [ObservableProperty]
    private string _curveDataText = string.Empty;

    [ObservableProperty]
    private int _curveMinValue;

    [ObservableProperty]
    private int _curveMaxValue;

    [ObservableProperty]
    private int _curveAvgValue;

    /// <summary>自然风曲线数据 (128字节)</summary>
    public ObservableCollection<byte> CurveData { get; } = new(FanConstants.DefaultNatureWindCurve);

    // ===== 档位风速标定 =====
    [ObservableProperty]
    private int _speed1 = FanConstants.DefaultGearSpeeds[0];

    [ObservableProperty]
    private int _speed2 = FanConstants.DefaultGearSpeeds[1];

    [ObservableProperty]
    private int _speed3 = FanConstants.DefaultGearSpeeds[2];

    [ObservableProperty]
    private int _speed4 = FanConstants.DefaultGearSpeeds[3];

    [ObservableProperty]
    private string _gearSpeedsDisplayText = "未读取";

    // ===== 错误提示 =====
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    // ===== 状态刷新 =====
    private PeriodicTimer? _refreshTimer;
    private CancellationTokenSource? _refreshCts;

    public MainViewModel(FanControlService fanService)
    {
        _fanService = fanService;
        _fanService.ConnectionStateChanged += OnConnectionStateChanged;
        UpdateCurveDisplay();
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsConnected = isConnected;
            ConnectionStatusText = isConnected ? "已连接" : "未连接";
        });
    }

    // ==================== 连接管理 ====================

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            ClearError();
            ConnectionStatusText = "正在扫描...";
            var info = await _fanService.ConnectAsync();
            DeviceInfo = $"{info.Name} ({info.Id})";
            ConnectionStatusText = "已连接";
            await RefreshStatusAsync();
            await ReadGearSpeeds();
            StartRealTimeRefresh();
        }
        catch (Exception ex)
        {
            ShowError($"连接失败: {ex.Message}");
            ConnectionStatusText = "连接失败";
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            StopRealTimeRefresh();
            await _fanService.DisconnectAsync();
            DeviceInfo = string.Empty;
            FanSpeedText = "-- %";
            VoltageText = "-- V";
            BatteryVoltageText = "-- V";
            CurrentText = "-- A";
            PowerText = "-- W";
        }
        catch (Exception ex)
        {
            ShowError($"断开失败: {ex.Message}");
        }
    }

    // ==================== 档位控制 ====================

    [RelayCommand]
    private async Task SetPowerAsync(string modeStr)
    {
        try
        {
            ClearError();
            var mode = (PowerMode)Enum.Parse(typeof(PowerMode), modeStr);
            await _fanService.SetPowerAsync(mode);
        }
        catch (Exception ex)
        {
            ShowError($"设置档位失败: {ex.Message}");
        }
    }

    // ==================== 转速控制 ====================

    partial void OnFanSpeedChanged(int value)
    {
        if (_fanService.IsConnected)
        {
            _ = _fanService.SetFanSpeedAsync(value);
        }
    }

    [RelayCommand]
    private async Task ApplyManualSpeedAsync()
    {
        try
        {
            ClearError();
            FanSpeed = ManualSpeedInput;
            await _fanService.SetFanSpeedAsync(FanSpeed);
        }
        catch (Exception ex)
        {
            ShowError($"应用转速失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResetSpeedAsync()
    {
        try
        {
            FanSpeed = FanConstants.DefaultFanSpeed;
            ManualSpeedInput = FanConstants.DefaultFanSpeed;
            await _fanService.SetFanSpeedAsync(FanSpeed);
        }
        catch (Exception ex)
        {
            ShowError($"重置转速失败: {ex.Message}");
        }
    }

    // ==================== 定时控制 ====================

    [RelayCommand]
    private async Task SetTimerAsync()
    {
        try
        {
            ClearError();
            await _fanService.SetTimerMinutesAsync(TimerMinutes);
            RemainingTimeText = $"已设置: {TimerMinutes}分钟";
        }
        catch (Exception ex)
        {
            ShowError($"设置定时失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SetPresetTimerAsync(int hours)
    {
        try
        {
            ClearError();
            await _fanService.SetTimerAsync(hours * 3600);
            RemainingTimeText = $"已设置: {hours}小时";
        }
        catch (Exception ex)
        {
            ShowError($"设置定时失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CancelTimerAsync()
    {
        try
        {
            await _fanService.CancelTimerAsync();
            RemainingTimeText = "已取消";
        }
        catch (Exception ex)
        {
            ShowError($"取消定时失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReadRemainingTimeAsync()
    {
        try
        {
            var remaining = await _fanService.ReadRemainingTimeAsync();
            RemainingTimeText = remaining.HasValue
                ? $"剩余: {remaining.Value.Hours}时{remaining.Value.Minutes}分{remaining.Value.Seconds}秒"
                : "无定时";
        }
        catch (Exception ex)
        {
            RemainingTimeText = "读取失败";
            ShowError($"读取定时失败: {ex.Message}");
        }
    }

    // ==================== 自然风控制 ====================

    /// <summary>
    /// ToggleSwitch 切换自然风
    /// </summary>
    [RelayCommand]
    private async Task ToggleNatureWindAsync()
    {
        try
        {
            ClearError();
            // ToggleSwitch 已经通过绑定修改了 NatureWindEnabled
            NatureWindButtonText = NatureWindEnabled ? "自然风 ON" : "自然风 OFF";
            await _fanService.SetNatureWindAsync(NatureWindEnabled);
        }
        catch (Exception ex)
        {
            // 回滚状态
            NatureWindEnabled = !NatureWindEnabled;
            NatureWindButtonText = NatureWindEnabled ? "自然风 ON" : "自然风 OFF";
            ShowError($"切换自然风失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void GenerateRandomCurve()
    {
        var mode = NatureWindCurveMode.None;
        if (SmoothMode) mode |= NatureWindCurveMode.Smooth;
        if (QuietMode) mode |= NatureWindCurveMode.Quiet;
        if (StrongMode) mode |= NatureWindCurveMode.Strong;
        var curve = _fanService.GenerateRandomCurve(mode);
        UpdateCurveData(curve);
    }

    [RelayCommand]
    private async Task ApplyEditedCurveAsync()
    {
        try
        {
            ClearError();
            var data = CurveData.ToArray();
            await _fanService.WriteNatureWindCurveAsync(data);
        }
        catch (Exception ex)
        {
            ShowError($"应用曲线失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReadNatureWindCurveAsync()
    {
        try
        {
            ClearError();
            var data = await _fanService.ReadNatureWindCurveAsync();
            UpdateCurveData(data);
        }
        catch (Exception ex)
        {
            ShowError($"读取曲线失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RestoreDefaultCurve()
    {
        var defaultCurve = (byte[])FanConstants.DefaultNatureWindCurve.Clone();
        UpdateCurveData(defaultCurve);
    }

    // ==================== 档位风速标定 ====================

    [RelayCommand]
    private async Task SetGearSpeedsAsync()
    {
        try
        {
            ClearError();
            var speeds = new[] { Speed1, Speed2, Speed3, Speed4 };
            await _fanService.SetGearSpeedsAsync(speeds);
            GearSpeedsDisplayText = $"{Speed1}% / {Speed2}% / {Speed3}% / {Speed4}%";
        }
        catch (Exception ex)
        {
            ShowError($"设置风速失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReadGearSpeeds()
    {
        try
        {
            var speeds = await _fanService.ReadGearSpeedsAsync();
            Speed1 = speeds[0];
            Speed2 = speeds[1];
            Speed3 = speeds[2];
            Speed4 = speeds[3];
            GearSpeedsDisplayText = $"{speeds[0]}% / {speeds[1]}% / {speeds[2]}% / {speeds[3]}%";
        }
        catch (Exception ex)
        {
            GearSpeedsDisplayText = "读取失败";
            ShowError($"读取风速失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResetGearSpeedsAsync()
    {
        Speed1 = FanConstants.DefaultGearSpeeds[0];
        Speed2 = FanConstants.DefaultGearSpeeds[1];
        Speed3 = FanConstants.DefaultGearSpeeds[2];
        Speed4 = FanConstants.DefaultGearSpeeds[3];
        await SetGearSpeedsAsync();
    }

    // ==================== 实时状态刷新 ====================

    private void StartRealTimeRefresh()
    {
        StopRealTimeRefresh();
        _refreshCts = new CancellationTokenSource();
        _refreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = RefreshLoop(_refreshCts.Token);
    }

    private void StopRealTimeRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    private async Task RefreshLoop(CancellationToken ct)
    {
        while (_refreshTimer != null && !ct.IsCancellationRequested)
        {
            try
            {
                await _refreshTimer.WaitForNextTickAsync(ct);
                await RefreshStatusAsync();
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            var status = await _fanService.ReadRealTimeStatusAsync();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                FanSpeedText = status.FanSpeed >= 0 ? $"{status.FanSpeed} %" : "-- %";
                VoltageText = status.Voltage >= 0 ? $"{status.Voltage:F2} V" : "-- V";
                BatteryVoltageText = status.BatteryVoltage >= 0 ? $"{status.BatteryVoltage:F2} V" : "-- V";
                CurrentText = status.Current >= 0 ? $"{status.Current:F2} A" : "-- A";
                PowerText = status.Power > 0 ? $"{status.Power:F2} W" : "-- W";
            });
        }
        catch { }
    }

    // ==================== 辅助方法 ====================

    private void UpdateCurveData(byte[] data)
    {
        CurveData.Clear();
        foreach (var b in data) CurveData.Add(b);
        UpdateCurveDisplay();
    }

    private void UpdateCurveDisplay()
    {
        if (CurveData.Count == 0) return;
        CurveMinValue = CurveData.Min();
        CurveMaxValue = CurveData.Max();
        CurveAvgValue = (int)CurveData.Average(d => (double)d);
        CurveDataText = string.Join(" ", CurveData);
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }
}

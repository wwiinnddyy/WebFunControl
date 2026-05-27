using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebFunControl.Models;
using WebFunControl.Services;

namespace WebFunControl.ViewModels;

/// <summary>
/// 主ViewModel — 聚合所有子功能模块
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly FanControlService _fanService;
    private readonly DeviceStore _deviceStore;

    // ===== 连接管理 =====
    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatusText = "未连接";

    [ObservableProperty]
    private string _deviceInfo = string.Empty;

    [ObservableProperty]
    private PairedDevice? _selectedDevice;

    [ObservableProperty]
    private bool _isScanning;

    /// <summary>已配对设备列表</summary>
    public ObservableCollection<PairedDevice> PairedDevices { get; } = [];

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

    public MainViewModel(FanControlService fanService, DeviceStore deviceStore)
    {
        _fanService = fanService;
        _deviceStore = deviceStore;
        _fanService.ConnectionStateChanged += OnConnectionStateChanged;
        UpdateCurveDisplay();

        // 加载已保存的配对设备
        _deviceStore.Load();
        foreach (var d in _deviceStore.Devices)
            PairedDevices.Add(d);
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsConnected = isConnected;
            ConnectionStatusText = isConnected ? "已连接" : "未连接";

            // 更新设备列表中的连接状态
            foreach (var d in PairedDevices)
            {
                d.IsConnected = isConnected && d == SelectedDevice;
            }
        });
    }

    // ==================== 连接管理 ====================

    /// <summary>
    /// 选中设备时触发连接
    /// </summary>
    partial void OnSelectedDeviceChanged(PairedDevice? value)
    {
        if (value == null) return;
        // 如果选中的不是"添加新设备"占位，则连接该设备
        if (value.Id != "__add_new__")
        {
            _ = ConnectToDeviceAsync(value);
        }
    }

    private async Task ConnectToDeviceAsync(PairedDevice device)
    {
        try
        {
            ClearError();
            ConnectionStatusText = "正在连接...";
            var info = await _fanService.ConnectAsync();
            DeviceInfo = $"{info.Name} ({info.Id})";
            ConnectionStatusText = "已连接";
            device.IsConnected = true;
            device.IsOnline = true;
            device.Id = info.Id;
            device.Name = info.Name;
            _deviceStore.AddOrUpdate(info.Id, info.Name);
            await RefreshStatusAsync();
            await ReadGearSpeeds();
            StartRealTimeRefresh();
        }
        catch (Exception ex)
        {
            device.IsConnected = false;
            device.IsOnline = false;
            ShowError($"连接失败: {ex.Message}");
            ConnectionStatusText = "连接失败";
        }
    }

    /// <summary>
    /// 扫描添加新设备
    /// </summary>
    [RelayCommand]
    private async Task AddNewDeviceAsync()
    {
        try
        {
            ClearError();
            IsScanning = true;
            ConnectionStatusText = "正在扫描...";
            var info = await _fanService.ConnectAsync();

            // 添加到配对列表
            _deviceStore.AddOrUpdate(info.Id, info.Name);
            var device = new PairedDevice { Id = info.Id, Name = info.Name, IsConnected = true, IsOnline = true };
            PairedDevices.Add(device);
            SelectedDevice = device;

            DeviceInfo = $"{info.Name} ({info.Id})";
            ConnectionStatusText = "已连接";
            await RefreshStatusAsync();
            await ReadGearSpeeds();
            StartRealTimeRefresh();
        }
        catch (Exception ex)
        {
            ShowError($"扫描失败: {ex.Message}");
            ConnectionStatusText = "扫描失败";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            StopRealTimeRefresh();
            await _fanService.DisconnectAsync();

            if (SelectedDevice != null)
                SelectedDevice.IsConnected = false;

            DeviceInfo = string.Empty;
            ConnectionStatusText = "未连接";
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

    /// <summary>
    /// 移除配对设备
    /// </summary>
    [RelayCommand]
    private void RemoveDevice(PairedDevice device)
    {
        if (device.IsConnected)
        {
            _ = DisconnectAsync();
        }
        PairedDevices.Remove(device);
        _deviceStore.Remove(device.Id);
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
            _ = SafeAsync(() => _fanService.SetFanSpeedAsync(value));
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

    [RelayCommand]
    private async Task ToggleNatureWindAsync()
    {
        try
        {
            ClearError();
            NatureWindButtonText = NatureWindEnabled ? "自然风 ON" : "自然风 OFF";
            await _fanService.SetNatureWindAsync(NatureWindEnabled);
        }
        catch (Exception ex)
        {
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

    private async Task SafeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowError(ex.Message));
        }
    }
}

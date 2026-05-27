using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace WebFunControl.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        var panel = this.Get<StackPanel>("RootPanel");

        // 关于
        panel.Children.Add(MakeSettingsExpander("关于", "应用信息", MakeAboutContent));

        // 错误提示
        var infoBar = new FAInfoBar
        {
            Title = "操作失败",
            IsClosable = true,
            Severity = FAInfoBarSeverity.Error,
        };
        infoBar.Bind(FAInfoBar.IsOpenProperty, new Avalonia.Data.Binding("HasError"));
        infoBar.Bind(FAInfoBar.MessageProperty, new Avalonia.Data.Binding("ErrorMessage"));
        panel.Children.Add(infoBar);

        // 连接设备
        panel.Children.Add(MakeSettingsExpander("连接设备", null, MakeConnectContent));

        // 实时状态
        panel.Children.Add(MakeSettingsExpander("实时状态", null, MakeStatusContent));

        // 档位控制
        panel.Children.Add(MakeSettingsExpander("档位控制", null, MakeGearContent));

        // 转速控制
        panel.Children.Add(MakeSettingsExpander("转速控制", null, MakeSpeedContent));

        // 自定义定时
        panel.Children.Add(MakeSettingsExpander("自定义定时", null, MakeTimerContent));

        // 自然风控制
        panel.Children.Add(MakeSettingsExpander("自然风", null, MakeNatureWindContent));

        // 档位风速设置
        panel.Children.Add(MakeSettingsExpander("档位风速设置", null, MakeGearSpeedContent));
    }

    private FASettingsExpander MakeSettingsExpander(string header, string? description, Func<Control> contentFactory)
    {
        var se = new FASettingsExpander { Header = header };
        if (description != null)
            se.Description = description;
        var content = contentFactory();
        se.Items.Add(new FASettingsExpanderItem { Content = content });
        return se;
    }

    private StackPanel MakeAboutContent()
    {
        var sp = new StackPanel { Spacing = 4, Margin = new Avalonia.Thickness(0, 8) };
        var ver = new TextBlock { FontSize = 12 };
        ver.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Version") { StringFormat = "版本: {0}" });
        sp.Children.Add(ver);
        var url = new TextBlock { FontSize = 12 };
        url.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("ApplicationProjectUrl"));
        sp.Children.Add(url);
        return sp;
    }

    private StackPanel MakeConnectContent()
    {
        var sp = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(0, 8) };
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var btnConnect = new Button { Content = "连接风扇" };
        btnConnect.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ConnectCommand"));
        btnConnect.Bind(Button.IsEnabledProperty, new Avalonia.Data.Binding("!IsConnected"));
        var btnDisconnect = new Button { Content = "断开连接" };
        btnDisconnect.Bind(Button.CommandProperty, new Avalonia.Data.Binding("DisconnectCommand"));
        btnDisconnect.Bind(Button.IsEnabledProperty, new Avalonia.Data.Binding("IsConnected"));
        btnRow.Children.AddRange([btnConnect, btnDisconnect]);
        sp.Children.Add(btnRow);

        var deviceInfo = new TextBlock { FontSize = 12 };
        deviceInfo.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("DeviceInfo"));
        deviceInfo.Bind(TextBlock.IsVisibleProperty, new Avalonia.Data.Binding("IsConnected"));
        sp.Children.Add(deviceInfo);
        return sp;
    }

    private StackPanel MakeStatusContent()
    {
        var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,*,*"), RowDefinitions = RowDefinitions.Parse("*,*"), Margin = new Avalonia.Thickness(0, 8), Width = 360 };

        string[] labels = ["转速", "系统电压", "电池电压", "电流", "功率", "剩余时间"];
        string[] bindings = ["FanSpeedText", "VoltageText", "BatteryVoltageText", "CurrentText", "PowerText", "RemainingTimeText"];
        int[] fontSizes = [15, 15, 15, 15, 15, 12];

        for (int i = 0; i < 6; i++)
        {
            var border = new Border { CornerRadius = new Avalonia.CornerRadius(6), Margin = new Avalonia.Thickness(3), Padding = new Avalonia.Thickness(8, 6) };
            var inner = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var label = new TextBlock { Text = labels[i], FontSize = 11, Opacity = 0.6, HorizontalAlignment = HorizontalAlignment.Center };
            var value = new TextBlock { FontSize = fontSizes[i], FontWeight = Avalonia.Media.FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
            value.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding(bindings[i]));
            inner.Children.AddRange([label, value]);
            border.Child = inner;
            Grid.SetRow(border, i / 3);
            Grid.SetColumn(border, i % 3);
            grid.Children.Add(border);
        }

        return new StackPanel { Children = { grid } };
    }

    private StackPanel MakeGearContent()
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Avalonia.Thickness(0, 8) };
        string[] gears = ["关机:Off", "1档:Gear1", "2档:Gear2", "3档:Gear3", "4档:Gear4"];
        foreach (var g in gears)
        {
            var parts = g.Split(':');
            var btn = new Button { Content = parts[0] };
            btn.Bind(Button.CommandProperty, new Avalonia.Data.Binding("SetPowerCommand"));
            btn.CommandParameter = parts[1];
            sp.Children.Add(btn);
        }
        return new StackPanel { Children = { sp } };
    }

    private StackPanel MakeSpeedContent()
    {
        var sp = new StackPanel { Spacing = 10, Margin = new Avalonia.Thickness(0, 8), MinWidth = 320 };

        var slider = new Slider { Minimum = 0, Maximum = 100 };
        slider.Bind(Slider.ValueProperty, new Avalonia.Data.Binding("FanSpeed"));
        sp.Children.Add(slider);

        var inputRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto"), ColumnSpacing = 8 };
        inputRow.Children.Add(new TextBlock { Text = "输入转速:", VerticalAlignment = VerticalAlignment.Center });
        var nb = new FANumberBox { Minimum = 0, Maximum = 100 };
        nb.Bind(FANumberBox.ValueProperty, new Avalonia.Data.Binding("ManualSpeedInput"));
        Grid.SetColumn(nb, 1);
        inputRow.Children.Add(nb);
        var pct = new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center, FontWeight = Avalonia.Media.FontWeight.SemiBold };
        Grid.SetColumn(pct, 2);
        inputRow.Children.Add(pct);
        sp.Children.Add(inputRow);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var apply = new Button { Content = "应用转速" };
        apply.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ApplyManualSpeedCommand"));
        var reset = new Button { Content = "重置默认(50%)" };
        reset.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ResetSpeedCommand"));
        btnRow.Children.AddRange([apply, reset]);
        sp.Children.Add(btnRow);

        return sp;
    }

    private StackPanel MakeTimerContent()
    {
        var sp = new StackPanel { Spacing = 10, Margin = new Avalonia.Thickness(0, 8), MinWidth = 320 };

        var inputRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto"), ColumnSpacing = 8 };
        inputRow.Children.Add(new TextBlock { Text = "定时（分钟）:", VerticalAlignment = VerticalAlignment.Center });
        var nb = new FANumberBox { Minimum = 1, Maximum = 480 };
        nb.Bind(FANumberBox.ValueProperty, new Avalonia.Data.Binding("TimerMinutes"));
        Grid.SetColumn(nb, 1);
        inputRow.Children.Add(nb);
        inputRow.Children.Add(new TextBlock { Text = "分", VerticalAlignment = VerticalAlignment.Center });
        sp.Children.Add(inputRow);

        var presetRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var h1 = new Button { Content = "1小时" }; h1.Bind(Button.CommandProperty, new Avalonia.Data.Binding("SetPresetTimerCommand")); h1.CommandParameter = 1;
        var h4 = new Button { Content = "4小时" }; h4.Bind(Button.CommandProperty, new Avalonia.Data.Binding("SetPresetTimerCommand")); h4.CommandParameter = 4;
        presetRow.Children.AddRange([h1, h4]);
        sp.Children.Add(presetRow);

        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var set = new Button { Content = "设置定时" }; set.Bind(Button.CommandProperty, new Avalonia.Data.Binding("SetTimerCommand"));
        var cancel = new Button { Content = "取消定时" }; cancel.Bind(Button.CommandProperty, new Avalonia.Data.Binding("CancelTimerCommand"));
        var read = new Button { Content = "读取剩余" }; read.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ReadRemainingTimeCommand"));
        actionRow.Children.AddRange([set, cancel, read]);
        sp.Children.Add(actionRow);

        return sp;
    }

    private StackPanel MakeNatureWindContent()
    {
        var sp = new StackPanel { Spacing = 10, Margin = new Avalonia.Thickness(0, 8), MinWidth = 320 };

        // 开关
        var switchRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto") };
        switchRow.Children.Add(new TextBlock { Text = "自然风开关", VerticalAlignment = VerticalAlignment.Center, FontWeight = Avalonia.Media.FontWeight.SemiBold });
        var ts = new ToggleSwitch { OnContent = "", OffContent = "" };
        ts.Bind(ToggleSwitch.IsCheckedProperty, new Avalonia.Data.Binding("NatureWindEnabled"));
        ts.Bind(ToggleSwitch.CommandProperty, new Avalonia.Data.Binding("ToggleNatureWindCommand"));
        Grid.SetColumn(ts, 1);
        switchRow.Children.Add(ts);
        sp.Children.Add(switchRow);

        // 曲线模式
        sp.Children.Add(new TextBlock { Text = "曲线模式", FontWeight = Avalonia.Media.FontWeight.SemiBold });
        var modeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        var cbSmooth = new CheckBox { Content = "平滑" }; cbSmooth.Bind(CheckBox.IsCheckedProperty, new Avalonia.Data.Binding("SmoothMode"));
        var cbQuiet = new CheckBox { Content = "安静" }; cbQuiet.Bind(CheckBox.IsCheckedProperty, new Avalonia.Data.Binding("QuietMode"));
        var cbStrong = new CheckBox { Content = "强劲" }; cbStrong.Bind(CheckBox.IsCheckedProperty, new Avalonia.Data.Binding("StrongMode"));
        modeRow.Children.AddRange([cbSmooth, cbQuiet, cbStrong]);
        sp.Children.Add(modeRow);

        var genBtn = new Button { Content = "随机生成曲线", HorizontalAlignment = HorizontalAlignment.Stretch };
        genBtn.Bind(Button.CommandProperty, new Avalonia.Data.Binding("GenerateRandomCurveCommand"));
        sp.Children.Add(genBtn);

        // 曲线数据摘要
        var summaryBorder = new Border { CornerRadius = new Avalonia.CornerRadius(6), Padding = new Avalonia.Thickness(12) };
        var summarySp = new StackPanel { Spacing = 4 };
        summarySp.Children.Add(new TextBlock { Text = "曲线数据概览", FontSize = 13, FontWeight = Avalonia.Media.FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });

        var statsGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,*,*"), Opacity = 0.8 };
        var minTb = new TextBlock { FontSize = 12 }; minTb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("CurveMinValue") { StringFormat = "最小值: {0}" });
        var maxTb = new TextBlock { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center }; maxTb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("CurveMaxValue") { StringFormat = "最大值: {0}" });
        var avgTb = new TextBlock { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right }; avgTb.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("CurveAvgValue") { StringFormat = "平均值: {0}" });
        statsGrid.Children.AddRange([minTb, maxTb, avgTb]);
        summarySp.Children.Add(statsGrid);

        var pb = new ProgressBar { Minimum = 20, Maximum = 90, HorizontalAlignment = HorizontalAlignment.Stretch };
        pb.Bind(ProgressBar.ValueProperty, new Avalonia.Data.Binding("CurveAvgValue"));
        summarySp.Children.Add(pb);
        summaryBorder.Child = summarySp;
        sp.Children.Add(summaryBorder);

        // 曲线文本编辑
        var tb = new TextBox { PlaceholderText = "自然风曲线数据（空格分隔，范围20-90）", FontFamily = FontFamily.Parse("Consolas"), FontSize = 11 };
        tb.Bind(TextBox.TextProperty, new Avalonia.Data.Binding("CurveDataText"));
        sp.Children.Add(tb);

        // 操作按钮
        var actionGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,*,*"), ColumnSpacing = 8 };
        var applyCurve = new Button { Content = "应用曲线" }; applyCurve.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ApplyEditedCurveCommand"));
        var restoreCurve = new Button { Content = "还原默认" }; restoreCurve.Bind(Button.CommandProperty, new Avalonia.Data.Binding("RestoreDefaultCurveCommand"));
        var readCurve = new Button { Content = "读取曲线" }; readCurve.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ReadNatureWindCurveCommand"));
        Grid.SetColumn(restoreCurve, 1);
        Grid.SetColumn(readCurve, 2);
        actionGrid.Children.AddRange([applyCurve, restoreCurve, readCurve]);
        sp.Children.Add(actionGrid);

        return sp;
    }

    private StackPanel MakeGearSpeedContent()
    {
        var sp = new StackPanel { Spacing = 10, Margin = new Avalonia.Thickness(0, 8), MinWidth = 320 };

        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto"),
            RowDefinitions = RowDefinitions.Parse("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 8,
            RowSpacing = 8
        };

        string[] labels = ["1档风速:", "2档风速:", "3档风速:", "4档风速:"];
        string[] bindings = ["Speed1", "Speed2", "Speed3", "Speed4"];

        for (int i = 0; i < 4; i++)
        {
            var label = new TextBlock { Text = labels[i], VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(label, i);
            grid.Children.Add(label);

            var nb = new FANumberBox { Minimum = 20, Maximum = 100 };
            nb.Bind(FANumberBox.ValueProperty, new Avalonia.Data.Binding(bindings[i]));
            Grid.SetRow(nb, i);
            Grid.SetColumn(nb, 1);
            grid.Children.Add(nb);

            var pct = new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center, FontWeight = Avalonia.Media.FontWeight.SemiBold };
            Grid.SetRow(pct, i);
            Grid.SetColumn(pct, 2);
            grid.Children.Add(pct);
        }
        sp.Children.Add(grid);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var apply = new Button { Content = "应用设置" }; apply.Bind(Button.CommandProperty, new Avalonia.Data.Binding("SetGearSpeedsCommand"));
        var reset = new Button { Content = "恢复默认" }; reset.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ResetGearSpeedsCommand"));
        var read = new Button { Content = "读取当前" }; read.Bind(Button.CommandProperty, new Avalonia.Data.Binding("ReadGearSpeedsCommand"));
        btnRow.Children.AddRange([apply, reset, read]);
        sp.Children.Add(btnRow);

        return sp;
    }
}

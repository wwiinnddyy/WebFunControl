using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace WebFunControl.Views;

public partial class AdvancedPage : UserControl
{
    public AdvancedPage()
    {
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        var panel = this.Get<StackPanel>("RootPanel");

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
            Grid.SetColumn(pct, i);
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

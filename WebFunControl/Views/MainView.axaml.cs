using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace WebFunControl.Views;

public partial class MainView : UserControl
{
    private Button? _currentTab;

    public MainView()
    {
        InitializeComponent();
        // 默认选中首页
        SwitchTab("Home");
    }

    private void OnTabClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            SwitchTab(tag);
        }
    }

    private void SwitchTab(string tag)
    {
        var grid = this.Get<Grid>("ContentGrid");
        grid.Children.Clear();

        var page = tag switch
        {
            "Home" => (Control)new HomePage(),
            "Advanced" => new AdvancedPage(),
            "About" => new AboutPage(),
            _ => new HomePage()
        };

        page.DataContext = DataContext;
        grid.Children.Add(page);

        // 更新 Tab 高亮
        UpdateTabHighlight(tag);
    }

    private void UpdateTabHighlight(string activeTag)
    {
        var tabs = new[] { ("Home", "TabHome", "TabHomeLabel"), ("Advanced", "TabAdvanced", "TabAdvancedLabel"), ("About", "TabAbout", "TabAboutLabel") };
        foreach (var (tag, btnName, labelName) in tabs)
        {
            var btn = this.Get<Button>(btnName);
            var label = this.Get<TextBlock>(labelName);
            if (tag == activeTag)
            {
                btn.FontWeight = FontWeight.Bold;
                label.Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0x66, 0x7E, 0xEA));
            }
            else
            {
                btn.FontWeight = FontWeight.Normal;
                label.Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0x6C, 0x75, 0x7D));
            }
        }
    }
}

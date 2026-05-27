using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;

namespace WebFunControl.Views;

public partial class MainWindow : FAAppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        NavList.SelectedIndex = 0;
        UpdateConnectionStatus(false);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ViewModels.MainViewModel vm)
        {
            // 监听连接状态变化
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ViewModels.MainViewModel.IsConnected))
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateConnectionStatus(vm.IsConnected));
            };
            UpdateConnectionStatus(vm.IsConnected);
        }
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is ListBoxItem item)
        {
            var tag = item.Tag?.ToString();
            if (tag != null) NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
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
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        var dot = this.Get<Ellipse>("StatusDot");
        var text = this.Get<TextBlock>("StatusText");

        dot.Fill = isConnected
            ? new SolidColorBrush(Color.FromRgb(0x00, 0xB8, 0x94))
            : new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
        text.Text = isConnected ? "已连接" : "未连接";
    }
}

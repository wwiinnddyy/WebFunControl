using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WebFunControl.Services;
using WebFunControl.Services.Ble;
using WebFunControl.ViewModels;
using WebFunControl.Views;

namespace WebFunControl;

public class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 注册DI服务 — 使用延迟初始化避免BLE平台实现在构造时崩溃
        var services = new ServiceCollection();
        services.AddSingleton<IBleService>(sp => new PluginBleService());
        services.AddSingleton<DeviceStore>();
        services.AddSingleton<FanControlService>();
        services.AddSingleton<MainViewModel>();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activityLifetime)
        {
            activityLifetime.MainViewFactory = () =>
            {
                var vm = Services.GetRequiredService<MainViewModel>();
                return new MainView { DataContext = vm };
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var vm = Services.GetRequiredService<MainViewModel>();
            singleView.MainView = new MainView { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

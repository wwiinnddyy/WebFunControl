using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace WebFunControl.Android;

[Activity(
    Label = "WebFunControl",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
    ScreenOrientation = ScreenOrientation.Portrait)]
public class MainActivity : AvaloniaMainActivity
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return builder
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}

using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace WebFunControl.Android;

[Activity(
    Label = "WebFunControl",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.Portrait)]
public class MainActivity : AvaloniaMainActivity
{
    private const int BluetoothPermissionRequestCode = 100;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestBluetoothPermissions();
    }

    private void RequestBluetoothPermissions()
    {
        // Android 12+ 需要运行时请求 BLE 权限
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            var permissions = new[]
            {
                "android.permission.BLUETOOTH_SCAN",
                "android.permission.BLUETOOTH_CONNECT",
                "android.permission.ACCESS_FINE_LOCATION"
            };
            RequestPermissions(permissions, BluetoothPermissionRequestCode);
        }
        else
        {
            RequestPermissions(new[] { "android.permission.ACCESS_FINE_LOCATION" }, BluetoothPermissionRequestCode);
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        // 权限结果处理 — 即使拒绝也不崩溃，只在连接时提示用户
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}

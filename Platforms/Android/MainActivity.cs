using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ControlRemotoBT;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        RequestNecessaryPermissions();
    }

    private void RequestNecessaryPermissions()
    {
        var permissions = new List<string>();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            permissions.Add(Manifest.Permission.BluetoothScan);
            permissions.Add(Manifest.Permission.BluetoothConnect);
        }
        else
        {
            permissions.Add(Manifest.Permission.Bluetooth);
            permissions.Add(Manifest.Permission.BluetoothAdmin);
        }

        // En todas las versiones: ubicación necesaria para escaneo BLE
        permissions.Add(Manifest.Permission.AccessFineLocation);

        if (permissions.Count > 0)
        {
            RequestPermissions(permissions.ToArray(), 0);
        }
    }
}
using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Avalonia.Android;
using Avalonia.Controls.Android;

namespace Avalonia.Controls.WebView.Samples.Android;

[Activity(
    Label = "Avalonia.Controls.WebView.Samples",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    LaunchMode = LaunchMode.SingleTask,
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]

public class MainActivity : AvaloniaMainActivity<App>
{

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
        {
        base.OnDestroy();
        }
    }

[Activity(
    Label = "Avalonia.Controls.WebView.Samples",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    Exported = true)]
[IntentFilter(actions: ["android.intent.action.VIEW"], Categories = ["android.intent.category.DEFAULT", "android.intent.category.BROWSABLE"], DataScheme = "com.avaloniaui.webview.samples", DataHost = "oauth2redirect")]
public class RedirectActivity : RedirectUriReceiverActivity
{
}

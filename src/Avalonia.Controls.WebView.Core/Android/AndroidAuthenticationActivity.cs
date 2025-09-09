#if ANDROID
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;

namespace Avalonia.Controls.Android
{
    [Activity(
    ExcludeFromRecents = true,
    LaunchMode = LaunchMode.SingleTask)]
    internal class AndroidAuthenticationActivity : Activity
    {
        private int _requestCode;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if(_requestCode != 0)
            {
                // Web auth browser was navigated from and thus cancelled.
                AndroidWebAuthenticationBroker.SetWebAuthenticationResult(_requestCode, Result.Canceled, null);
                Finish();
                return;
            }

            if (this.Intent?.Data is { } uri)
            {
                _requestCode = this.Intent?.GetIntExtra(AndroidWebAuthenticationBroker.RequestId, 0) ?? 0;
                var intent = new Intent(Intent.ActionView, null);

                _ = intent.SetData(global::Android.Net.Uri.Parse(uri.ToString()));

                this.StartActivity(Intent.CreateChooser(intent, "Select Browser"));
            }
            else
            {
                Finish();
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            var result = intent?.Data is not null ? Result.Ok : Result.Canceled;

            AndroidWebAuthenticationBroker.SetWebAuthenticationResult(_requestCode, result, intent);
            Finish();
        }
    }
}
#endif

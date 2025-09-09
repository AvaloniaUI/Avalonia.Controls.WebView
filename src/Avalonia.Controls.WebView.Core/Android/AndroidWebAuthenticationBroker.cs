#if ANDROID
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Avalonia.Android;

namespace Avalonia.Controls.Android;

[SupportedOSPlatform("android")]
internal static class AndroidWebAuthenticationBroker
{
    internal const string RequestId = "REQUEST_ID";
    private record AuthenticationData(Uri RedirectUri, TaskCompletionSource<Uri> TaskSource);

    private static readonly Dictionary<int, AuthenticationData> s_pendingAuthentications = [];

    public static async Task<Uri> AuthenticateAsync(TopLevel topLevel, Uri requestUri, Uri redirectUri)
    {
        var activity = GetActivity(topLevel) ?? throw new InvalidOperationException("Cannot get Android Activity");

        if (activity is not IAvaloniaActivity avActivity)
        {
            throw new InvalidOperationException("Activity must implement IAvaloniaActivity");
        }

        var requestCode = Random.Shared.Next(10000, 99999);
        var authData = new AuthenticationData(redirectUri, new TaskCompletionSource<Uri>());
        s_pendingAuthentications[requestCode] = authData;
        try
        {
            var intent = new Intent(activity, typeof(AndroidAuthenticationActivity));
            _ = intent.PutExtra(RequestId, requestCode);
            _ = intent.SetData(global::Android.Net.Uri.Parse(requestUri.ToString()));

            activity.StartActivity(intent);

            return await authData.TaskSource.Task;
        }
        finally
        {
            _ = s_pendingAuthentications.Remove(requestCode);
        }
    }

    internal static void SetWebAuthenticationResult(int requestCode, Result resultCode, Intent? data)
    {
        if (!s_pendingAuthentications.TryGetValue(requestCode, out var authData))
        {
            return;
        }

        if (resultCode == Result.Ok && data?.Data is not null)
        {
            // TODO we can filter by RedirectUri, should we?
            var uri = new Uri(data.Data.ToString()!);
            _ = authData.TaskSource.TrySetResult(uri);
        }
        else
        {
            _ = authData.TaskSource.TrySetCanceled();
        }

        _ = s_pendingAuthentications.Remove(requestCode);
    }

    private static Activity? GetActivity(TopLevel topLevel)
    {
        return topLevel.TryGetPlatformHandle() is AndroidViewControlHandle platformHandle
            ? GetActivity(platformHandle.View)
            : GetAvaloniaView(topLevel) is { } view
                ? GetActivity(view) : null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075")]
    private static global::Android.Views.View? GetAvaloniaView(TopLevel topLevel)
    {
        var implType = topLevel.PlatformImpl?.GetType();
        var view = implType?.GetProperty("View")?.GetValue(topLevel.PlatformImpl);
        return view as global::Android.Views.View;
    }

    private static Activity? GetActivity(global::Android.Views.View view)
    {
        var context = view.Context;
        return context as Activity ?? GetActivityFromContext(context);

        static Activity? GetActivityFromContext(Context? context)
        {
            while (context is ContextWrapper wrapper)
            {
                if (wrapper is Activity activity)
                {
                    return activity;
                }

                context = wrapper.BaseContext;
            }
            return null;
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Utils;
using static Avalonia.Controls.Gtk.GtkInterop;

namespace Avalonia.Controls.Gtk;

internal sealed class Soup3HttpHeaders(IntPtr headers, bool immutable) : INativeHttpRequestHeaders
{
    public bool Immutable => immutable;

    public bool TryClear()
    {
        soup_message_headers_clear(headers);
        return true;
    }

    public unsafe bool TryGetCount(out int count)
    {
        int[] boxedCount = [0];
        var userData = GCHandle.Alloc(boxedCount);
        try
        {
            soup_message_headers_foreach(headers, &MessageHeadersForeachFunc, GCHandle.ToIntPtr(userData));
        }
        finally
        {
            userData.Free();
        }

        count = boxedCount[0];
        return true;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void MessageHeadersForeachFunc(IntPtr namePtr, IntPtr valuePtr, IntPtr data)
        {
            if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not int[] count)
            {
                return;
            }

            count[0] += 1;
        }
    }

    public string? GetHeader(string name)
    {
        return Marshal.PtrToStringUTF8(soup_message_headers_get_list(headers, name));
    }

    public bool Contains(string name)
    {
        var value = Marshal.PtrToStringUTF8(soup_message_headers_get_one(headers, name));
        return !string.IsNullOrEmpty(value);
    }

    public bool TrySetHeader(string name, string value)
    {
        if (immutable)
            return false;
        soup_message_headers_replace(headers, name, value);
        return true;
    }

    public bool TryRemoveHeader(string name)
    {
        if (immutable)
            return false;
        if (Contains(name))
        {
            soup_message_headers_remove(headers, name);
            return true;
        }

        return false;
    }

    public unsafe INativeHttpHeadersCollectionIterator GetIterator()
    {
        var dictionary = new Dictionary<string, string>();
        var userData = GCHandle.Alloc(dictionary);
        try
        {
            soup_message_headers_foreach(headers, &MessageHeadersForeachFunc, GCHandle.ToIntPtr(userData));
        }
        finally
        {
            userData.Free();
        }

        return new DictionaryNativeHttpRequestHeaders.Iterator(dictionary);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void MessageHeadersForeachFunc(IntPtr namePtr, IntPtr valuePtr, IntPtr data)
        {
            if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not Dictionary<string, string> adapter)
            {
                return;
            }

            var name = Marshal.PtrToStringAnsi(namePtr);
            var value = Marshal.PtrToStringAnsi(valuePtr);
            if (!string.IsNullOrEmpty(name))
            {
                adapter[name] = value ?? "";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.VisualTree;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal sealed class GtkOffscreenAvaloniaWebViewAdapter : GtkOffscreenWebViewAdapter
{
    private static readonly unsafe IntPtr s_showOptionMenuCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, GdkEvent*, GdkRectangle*, IntPtr, int>)&ShowOptionMenuCallback);
    private static readonly unsafe IntPtr s_contextMenuCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, GdkEvent*, IntPtr, IntPtr, int>)&ContextMenuCallback);
    private static readonly unsafe IntPtr s_optionsMenuClosedCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&MenuClosedCallback);

    private GtkSignal? _showOptionMenuSignal;
    //private GtkSignal? _contextMenuSignal;
    private HashSet<IDisposable> _openedMenus = new();

    private GtkOffscreenAvaloniaWebViewAdapter(GtkWebViewEnvironmentRequestedEventArgs environmentArgs) : base(environmentArgs)
    {
        _showOptionMenuSignal = new GtkSignal(WebViewHandle, "show-option-menu", s_showOptionMenuCallback, this);
    }

    public Control? Parent { get; private set; }

    public static async Task<WebViewAdapter.OffscreenWebViewAdapterBuilder> CreateBuilder(
        GtkWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        var adapter = await RunOnGlibThreadAsync(() => new GtkOffscreenAvaloniaWebViewAdapter(environmentArgs));
        return (parent) =>
        {
            adapter.Parent = parent;
            return Task.FromResult<IWebViewAdapterWithOffscreenBuffer>(adapter);
        };
    }

    protected override void DisposeSafe(bool disposing)
    {
        if (disposing)
        {
            Interlocked.Exchange(ref _showOptionMenuSignal, null)?.Dispose();
            // Interlocked.Exchange(ref contextMenuSignal, null)?.Dispose();

            var menus = Interlocked.Exchange(ref _openedMenus, new());
            foreach (var menu in menus)
            {
                menu.Dispose();
            }
            menus.Clear();
        }
        base.DisposeSafe(disposing);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int ContextMenuCallback(IntPtr webview, IntPtr menu, GdkEvent* sourceEvent, IntPtr hitTest, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkOffscreenAvaloniaWebViewAdapter adapter)
        {
            return False;
        }

        return False;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int ShowOptionMenuCallback(IntPtr webview, IntPtr menu, GdkEvent* sourceEvent, GdkRectangle* rect, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkOffscreenAvaloniaWebViewAdapter adapter)
        {
            return False;
        }

        var isMouseRequest = sourceEvent is not null && sourceEvent->Type == GdkEventType.GDK_BUTTON_PRESS;
        var openMenuState = new GtkOptionsMenuState(menu, isMouseRequest, *rect, adapter);
        openMenuState.Open();

        return True;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void MenuClosedCallback(IntPtr menu, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkOptionsMenuState state)
        {
            return;
        }

        state.ClosedRequested();
    }

    private class GtkOptionsMenuState : IDisposable
    {
        private readonly bool _isMouseRequest;
        private readonly GdkRectangle _rect;
        private readonly GtkOffscreenAvaloniaWebViewAdapter _adapter;
        private readonly GtkSignal _closeSignal;
        private IntPtr _menu;
        private ContextMenu? _contextMenu;

        public GtkOptionsMenuState(IntPtr menu, bool isMouseRequest, GdkRectangle rect,
            GtkOffscreenAvaloniaWebViewAdapter adapter)
        {
            g_object_ref(menu);
            _menu = menu;
            _isMouseRequest = isMouseRequest;
            _rect = rect;
            _adapter = adapter;
            _closeSignal = new GtkSignal(menu, "close", s_optionsMenuClosedCallback, this);
        }

        public void ClosedRequested()
        {
            WebViewDispatcher.InvokeAsync(() => { _contextMenu?.Close(); });
        }

        public void Open()
        {
            _adapter._openedMenus.Add(this);
            var nativeMenuItems = ExtractMenu(_menu);

            WebViewDispatcher.InvokeAsync(() =>
            {
                var actualWebView = (Control?)_adapter.Parent?.GetVisualParent()!;
                var pixelRect = new PixelRect(_rect.x, _rect.y, _rect.width, _rect.height);
                _contextMenu = new ContextMenu
                {
                    Placement = PlacementMode.Bottom,
                    PlacementRect = pixelRect.ToRect(TopLevel.GetTopLevel(actualWebView)!.RenderScaling),
                    VerticalOffset = 4,
                    PlacementTarget = actualWebView,
                    DataContext = this
                };
                _contextMenu.Closed += static (el, _) =>
                {
                    if (el is ContextMenu { DataContext: GtkOptionsMenuState state })
                    {
                        state.Dispose(true, true);
                    }
                };

                string? currentGroup = null;
                foreach (var item in nativeMenuItems)
                {
                    if (item.GroupLabel)
                    {
                        currentGroup = item.Label;
                        if (_contextMenu.Items.Count > 0)
                        {
                            _contextMenu.Items.Add(new Separator());
                        }
                    }
                    else
                    {
                        var menuItem = new MenuItem
                        {
                            Header = item.Label,
                            IsEnabled = item.IsEnabled,
                            IsChecked = item.IsSelected,
                            ToggleType = item.ToggleType,
                            DataContext = (this, item.Index),
                            GroupName = item.GroupChild ? currentGroup : null,
                            [ToolTip.TipProperty] = item.Tooltip
                        };

                        _contextMenu.Items.Add(menuItem);

                        menuItem.Click += static (el, _) =>
                        {
                            if (el is MenuItem
                                {
                                    IsChecked: true,
                                    DataContext: ValueTuple<GtkOptionsMenuState, uint> state
                                })
                            {
                                RunOnGlibThreadAsync(() =>
                                {
                                    if (state.Item1._menu != IntPtr.Zero)
                                    {
                                        webkit_option_menu_activate_item(state.Item1._menu, state.Item2);
                                    }
                                });
                            }
                        };
                    }
                }

                _contextMenu.Open(actualWebView);
            });
        }

        public void Dispose()
        {
            Dispose(true, false);
        }

        private void Dispose(bool disposing, bool close)
        {
            var menu = Interlocked.Exchange(ref _menu, IntPtr.Zero);
            if (menu != IntPtr.Zero)
            {
                RunOnGlibThreadAsync(() =>
                {
                    if (close)
                    {
                        webkit_option_menu_close(menu);
                    }

                    g_object_unref(menu);

                    if (disposing)
                    {
                        _closeSignal.Dispose();
                        _adapter._openedMenus.Remove(this);
                    }
                });
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        ~GtkOptionsMenuState()
        {
            Dispose(false, false);
        }

        private static List<GtkMenuItemModel> ExtractMenu(IntPtr menuPtr)
        {
            var result = new List<GtkMenuItemModel>();
            var itemCount = webkit_option_menu_get_n_items(menuPtr);

            for (uint i = 0; i < itemCount; i++)
            {
                var itemPtr = webkit_option_menu_get_item(menuPtr, i);
                if (itemPtr == IntPtr.Zero)
                    continue;

                var groupLabel = webkit_option_menu_item_is_group_label(itemPtr);
                result.Add(new GtkMenuItemModel
                {
                    Index = i,
                    Label = Marshal.PtrToStringAnsi(webkit_option_menu_item_get_label(itemPtr)) ?? string.Empty,
                    Tooltip = Marshal.PtrToStringAnsi(webkit_option_menu_item_get_tooltip(itemPtr)),
                    IsEnabled = groupLabel || webkit_option_menu_item_is_enabled(itemPtr),
                    IsSelected = webkit_option_menu_item_is_selected(itemPtr),
                    ToggleType = groupLabel ? MenuItemToggleType.None : MenuItemToggleType.Radio,
                    GroupLabel = groupLabel,
                    GroupChild = webkit_option_menu_item_is_group_child(itemPtr)
                });
            }

            return result;
        }

        private class GtkMenuItemModel
        {
            public uint Index { get; init; }
            public string Label { get; init; } = string.Empty;
            public string? Tooltip { get; init; }
            public bool IsEnabled { get; init; }
            public bool IsSelected { get; init; }
            public MenuItemToggleType ToggleType { get; init; }
            public bool GroupLabel { get; init; }
            public bool GroupChild { get; init; }
        }
    }
}

using System;

namespace Avalonia.Controls.Macios.Interop;

internal abstract class NSValue(IntPtr handle, bool owns) : NSObject(handle, owns);

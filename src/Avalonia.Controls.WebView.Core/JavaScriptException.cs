using System;

namespace Avalonia.Controls;

internal class JavaScriptException(string message, Exception? innerException = null)
    : Exception(message, innerException);

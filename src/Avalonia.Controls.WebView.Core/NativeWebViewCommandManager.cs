namespace Avalonia.Controls;

public class NativeWebViewCommandManager
{
    private readonly IWebViewAdapterWithCommands _webViewAdapter;

    internal NativeWebViewCommandManager(IWebViewAdapterWithCommands webViewAdapter)
    {
        _webViewAdapter = webViewAdapter;
    }

    public void Copy() => _webViewAdapter.Copy();
    public void Cut() => _webViewAdapter.Cut();
    public void Paste() => _webViewAdapter.Paste();
    public void SelectAll() => _webViewAdapter.SelectAll();
    public void Undo() => _webViewAdapter.Undo();
    public void Redo() => _webViewAdapter.Redo();
}

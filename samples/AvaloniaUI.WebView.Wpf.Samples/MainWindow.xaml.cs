using System.Windows.Input;
using TabItem = System.Windows.Controls.TabItem;
using Window = System.Windows.Window;

namespace AvaloniaUI.WebView.Wpf.Samples
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void NativeWebView_OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
        {
            LogList.Text += "\r\nNativeWebView_OnNavigationCompleted " + e.Request;
            LogList.Text += "\r\nInvoking JS script with invokeCSharpAction";

            await ((NativeWebView)sender!).InvokeScript(""" invokeCSharpAction("{'key': 10}") """);
        }

        private void NativeWebView_OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
        {
            LogList.Text += "\r\nNativeWebView_OnNavigationStarted " + e.Request;
        }

        private void NativeWebView_OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
        {
            LogList.Text += "\r\nNativeWebView_OnWebMessageReceived " + e.Body;
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.P)
            {
                using (WebView.BeginReparenting())
                {
                    var currentTab = (TabItem)GridContainer.Parent!;
                    currentTab.Content = null;
                    var index = TabControl.Items.Add(new TabItem
                    {
                        Header = "New Tab",
                        Content = GridContainer
                    });
                    TabControl.SelectedIndex = index;
                }
            }
        }
    }
}

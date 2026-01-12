using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  public partial class HelpViewerWindow : Window
  {
    private WebView2 webView;
    private Task? _initTask;
    private bool _initStarted;
    private string? _pendingUrl;

    private const uint RPC_E_DISCONNECTED = 0x80010108;

    public HelpViewerWindow()
    {
      InitializeComponent();
      Loaded += async (_, __) => await EnsureInitializedAsync();
      Closed += (_, __) => webView?.Dispose();
    }

    private void InitializeComponent()
    {
      Width = 1024; Height = 768;
      MinWidth = 600; MinHeight = 600;
      Title = "Справочная система";
      webView = new WebView2();
      Content = webView;
    }

    public void Navigate(string url) => _ = NavigateAsync(url);
    public async Task NavigateAsync(string url)
    {
      if (webView.CoreWebView2 == null)
      {
        _pendingUrl = url;
        if (!_initStarted) _ = EnsureInitializedAsync();
        await (_initTask ?? Task.CompletedTask);
      }
      if (webView.CoreWebView2 != null)
      {
        webView.CoreWebView2.Navigate(url);
        _pendingUrl = null;
      }
    }

    private async Task EnsureInitializedAsync()
    {
      if (!Dispatcher.CheckAccess())
      {
        await Dispatcher.InvokeAsync(async () => await EnsureInitializedAsync(), DispatcherPriority.Normal);
        return;
      }
      if (_initStarted) { await (_initTask ?? Task.CompletedTask); return; }

      _initStarted = true;
      _initTask = InitAsync();
      await _initTask;
    }

    private async Task InitAsync()
    {
      try
      {
        if (!webView.IsLoaded)
          await webView.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);

        var fixedRoot = Path.Combine(AppContext.BaseDirectory, "Help", "WebView2Runtime");
        var runtimePath = ResolveFixedRuntimeFolder(fixedRoot);
        var userData = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
          "AskMkiM", "WebView2", "UserData");
        Directory.CreateDirectory(userData);

        try
        {
          var env = await CoreWebView2Environment.CreateAsync(runtimePath, userData, new CoreWebView2EnvironmentOptions());
          await webView.EnsureCoreWebView2Async(env);
        }
        catch (COMException ex) when ((uint)ex.HResult == RPC_E_DISCONNECTED)
        {
          var env = await CoreWebView2Environment.CreateAsync(null, userData, new CoreWebView2EnvironmentOptions("--disable-gpu"));
          await webView.EnsureCoreWebView2Async(env);
        }

        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

        if (!string.IsNullOrWhiteSpace(_pendingUrl))
          await NavigateAsync(_pendingUrl!);
      }
      catch (Exception ex)
      {
        LogException(ex);
        MessageBox.Show("Ошибка инициализации справки: " + ex.Message,
          "Ошибка WebView2", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private static string ResolveFixedRuntimeFolder(string fixedRoot)
    {
      if (File.Exists(Path.Combine(fixedRoot, "msedgewebview2.exe")))
        return fixedRoot;

      if (Directory.Exists(fixedRoot))
      {
        var candidate = Directory.GetDirectories(fixedRoot)
          .Where(d => File.Exists(Path.Combine(d, "msedgewebview2.exe")))
          .OrderByDescending(Path.GetFileName)
          .FirstOrDefault();
        if (candidate != null) return candidate;
      }

      return fixedRoot;
    }
  }
}

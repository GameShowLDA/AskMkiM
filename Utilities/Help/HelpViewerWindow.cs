using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Utilities.Help
{
  public partial class HelpViewerWindow : Window
  {
    private WebView2 webView;

    public HelpViewerWindow()
    {
      InitializeComponent();
      InitializeWebView();
    }

    private void InitializeComponent()
    {
      Width = 1024;
      Height = 768;
      MinHeight = 800;
      MinWidth = 600;
      Title = "Справочная система";

      webView = new WebView2();
      Content = webView;
      Closed += (s, e) => webView.Dispose();
    }

    private async void InitializeWebView()
    {
      try
      {
        string runtimePath = @"Help\WebView2Runtime";
        string fullRuntimePath = Path.GetFullPath(runtimePath);
        LoggerUtility.LogInformation("📂 Инициализация WebView2. Путь к runtime: " + fullRuntimePath);
        LoggerUtility.LogInformation("📄 Существует msedgewebview2.exe: " +
            File.Exists(Path.Combine(fullRuntimePath, "msedgewebview2.exe")));

        var env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: fullRuntimePath
        );

        // Убедимся, что инициализация идёт строго через Environment
        await webView.EnsureCoreWebView2Async(env);

        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
      }
      catch (Exception ex)
      {
        LoggerUtility.LogException(ex);
        MessageBox.Show("Ошибка инициализации справки: " + ex.Message, "Ошибка WebView2",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public async void Navigate(string url)
    {
      try
      {
        if (webView.CoreWebView2 == null)
        {
          LoggerUtility.LogWarning("Попытка навигации до инициализации WebView2");
          return;
        }

        webView.Source = new Uri(url);
      }
      catch (Exception ex)
      {
        LoggerUtility.LogException(ex);
      }
    }
  }
}

using System;
using System.Windows;
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
      Title = "Справочная система";

      webView = new WebView2();
      Content = webView;
      Closed += (s, e) => webView.Dispose();
    }

    private async void InitializeWebView()
    {
      try
      {
        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка инициализации Help Viewer: {ex.Message}");
      }
    }

    public async void Navigate(string url)
    {
      if (webView.CoreWebView2 == null)
        await webView.EnsureCoreWebView2Async();

      webView.CoreWebView2.Navigate(url);
    }
  }
}
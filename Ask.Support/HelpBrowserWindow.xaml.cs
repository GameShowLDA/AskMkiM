using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Microsoft.Web.WebView2.Core;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support;

/// <summary>
/// Отображает HTML-справку в WebView2 с тематическим заголовком WPF.
/// </summary>
public partial class HelpBrowserWindow : Window
{
  private Uri? _pendingPage;
  private bool _closed;

  /// <summary>
  /// Создаёт окно справки и настраивает жизненный цикл браузера.
  /// </summary>
  public HelpBrowserWindow()
  {
    InitializeComponent();
    ApplyTheme(ThemeSettings.CurrentTheme is ThemeMode.Dark or ThemeMode.DarkCustom);
    Loaded += InitializeBrowser;
    Closed += (_, _) =>
    {
      _closed = true;
      Browser.Dispose();
    };
    StateChanged += (_, _) =>
    {
      bool maximized = WindowState == WindowState.Maximized;
      MaximizeButton.ToolTip = maximized ? "Восстановить" : "Развернуть";
      System.Windows.Automation.AutomationProperties.SetName(MaximizeButton, (string)MaximizeButton.ToolTip);
      MaximizeIcon.Data = (Geometry)FindResource(maximized ? "SemiIconRestore" : "SemiIconMaximize");
    };
  }

  /// <summary>
  /// Открывает страницу после готовности браузера; до инициализации сохраняет последний адрес.
  /// </summary>
  /// <param name="page">Адрес страницы локального сервера справки.</param>
  public void Navigate(Uri page)
  {
    _pendingPage = page;
    if (Browser.CoreWebView2 != null) Browser.CoreWebView2.Navigate(page.AbsoluteUri);
  }

  private async void InitializeBrowser(object sender, RoutedEventArgs e)
  {
    Loaded -= InitializeBrowser;
    try
    {
      var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AskMkiM", "Help", "WebView2");
      var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profile);
      if (_closed) return;
      await Browser.EnsureCoreWebView2Async(environment);
      if (_closed) return;
      Browser.CoreWebView2.WebMessageReceived += OnWebMessage;
      Browser.CoreWebView2.NavigationStarting += (_, args) =>
      {
        if (IsHelpAddress(args.Uri)) return;
        args.Cancel = true;
        OpenExternalLink(args.Uri);
      };
      Browser.CoreWebView2.NewWindowRequested += (_, args) =>
      {
        args.Handled = true;
        if (IsHelpAddress(args.Uri)) Browser.CoreWebView2.Navigate(args.Uri);
        else OpenExternalLink(args.Uri);
      };
      Browser.CoreWebView2.NavigationCompleted += (_, args) =>
      {
        StatusText.Text = args.IsSuccess ? string.Empty : "Не удалось загрузить страницу справки.";
        StatusText.Visibility = args.IsSuccess ? Visibility.Collapsed : Visibility.Visible;
      };
      if (_pendingPage != null) Browser.CoreWebView2.Navigate(_pendingPage.AbsoluteUri);
    }
    catch (Exception ex)
    {
      if (_closed) return;
      LogException(ex, customMessage: "Не удалось инициализировать WebView2 справки.");
      StatusText.Text = "Не удалось открыть справку. Проверьте наличие Microsoft Edge WebView2 Runtime. Подробности записаны в журнал.";
    }
  }

  private static bool IsHelpAddress(string address) =>
    Uri.TryCreate(address, UriKind.Absolute, out var uri) && HelpServer.BaseUrl is { } root &&
    uri.Scheme == root.Scheme && uri.Authority == root.Authority;

  private static void OpenExternalLink(string address)
  {
    if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
        uri.Scheme is not ("https" or "http" or "mailto" or "tel")) return;
    try
    {
      Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
    catch (Exception ex)
    {
      LogException(ex, customMessage: "Не удалось открыть ссылку из справки.");
    }
  }

  private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
  {
    if (!IsHelpAddress(e.Source)) return;
    try
    {
      using var message = JsonDocument.Parse(e.WebMessageAsJson);
      var root = message.RootElement;
      if (root.ValueKind != JsonValueKind.Object ||
          !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String ||
          type.GetString() != "mki:theme" || !root.TryGetProperty("theme", out var theme) ||
          theme.ValueKind != JsonValueKind.String) return;
      if (theme.GetString() is "dark" or "light") ApplyTheme(theme.GetString() == "dark");
    }
    catch (JsonException ex)
    {
      LogException(ex, customMessage: "Некорректное сообщение темы справки.");
    }
  }

  private void ApplyTheme(bool dark)
  {
    Resources["TopPanelButtonHighlightOpacity"] = dark ? 0.24 : 0.12;
    Resources["TopPanelButtonBevelBrush"] = dark
      ? new LinearGradientBrush(new GradientStopCollection
      {
        new GradientStop(Color.FromArgb(0x60, 0xB4, 0xC3, 0xD7), 0),
        new GradientStop(Color.FromArgb(0x24, 0xB4, 0xC3, 0xD7), 0.4),
        new GradientStop(Color.FromArgb(0x10, 0xB4, 0xC3, 0xD7), 1)
      }, new Point(0, 0), new Point(1, 1))
      : Brushes.Transparent;
    Resources["HelpBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#151E28" : "#F2F6FC"));
    Resources["HelpForeground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E8EEF7" : "#223246"));
    Resources["HelpBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#324255" : "#D4DEEA"));
    Resources["HelpHover"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#243241" : "#EAF0F8"));
  }

  private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
  private void Maximize_Click(object sender, RoutedEventArgs e)
  {
    if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
    else SystemCommands.MaximizeWindow(this);
  }
  private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

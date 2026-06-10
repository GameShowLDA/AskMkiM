using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Models;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ask.Diagnostics.Collectors
{
  public sealed class ScreenshotCollector : ICrashDataCollector
  {
    private const int Srccopy = 0x00CC0020;
    private const int PwRenderFullContent = 0x00000002;

    private readonly IOptions<CrashPackageOptions> _options;

    public ScreenshotCollector(IOptions<CrashPackageOptions> options)
    {
      _options = options;
    }

    public string Name => "Screenshot";

    public int Order => 200;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      if (!_options.Value.IncludeScreenshot)
      {
        return;
      }

      var application = Application.Current;
      var dispatcher = application?.Dispatcher;
      if (application == null || dispatcher == null)
      {
        await WriteInfoAsync(context, "WPF Application.Current or Dispatcher is not available.", cancellationToken).ConfigureAwait(false);
        return;
      }

      var outputPath = Path.Combine(context.PackageDirectory, "screenshot.png");
      if (dispatcher.CheckAccess())
      {
        SaveApplicationWindowScreenshot(application, context, outputPath);
        return;
      }

      var operation = dispatcher.InvokeAsync(
        () => SaveApplicationWindowScreenshot(application, context, outputPath),
        DispatcherPriority.Send);

      await operation.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void SaveApplicationWindowScreenshot(Application application, CrashContext context, string outputPath)
    {
      var window = FindCaptureWindow(application);
      if (window == null)
      {
        WriteInfo(context, "No visible WPF window was found.");
        return;
      }

      try
      {
        if (TrySaveWindowScreenshot(window, outputPath))
        {
          return;
        }

        if (TrySaveRenderedWindow(window, outputPath))
        {
          return;
        }

        WriteInfo(context, $"Failed to capture window '{window.GetType().FullName}'.");
      }
      catch (Exception ex)
      {
        WriteInfo(context, $"Screenshot capture failed: {ex}");
        throw;
      }
    }

    private static Window? FindCaptureWindow(Application application)
    {
      return application.Windows
          .OfType<Window>()
          .Where(static window => window.IsVisible)
          .OrderByDescending(static window => window.IsActive)
          .ThenByDescending(static window => ReferenceEquals(window, Application.Current?.MainWindow))
          .FirstOrDefault()
        ?? application.MainWindow;
    }

    private static bool TrySaveWindowScreenshot(Window window, string outputPath)
    {
      var hwnd = new WindowInteropHelper(window).Handle;
      if (hwnd == IntPtr.Zero)
      {
        return false;
      }

      if (!GetWindowRect(hwnd, out var rect))
      {
        return false;
      }

      var width = rect.Right - rect.Left;
      var height = rect.Bottom - rect.Top;
      if (width <= 0 || height <= 0)
      {
        return false;
      }

      var windowDc = GetWindowDC(hwnd);
      if (windowDc == IntPtr.Zero)
      {
        return false;
      }

      var memoryDc = IntPtr.Zero;
      var bitmap = IntPtr.Zero;
      var oldBitmap = IntPtr.Zero;

      try
      {
        memoryDc = CreateCompatibleDC(windowDc);
        if (memoryDc == IntPtr.Zero)
        {
          return false;
        }

        bitmap = CreateCompatibleBitmap(windowDc, width, height);
        if (bitmap == IntPtr.Zero)
        {
          return false;
        }

        oldBitmap = SelectObject(memoryDc, bitmap);
        var printed = PrintWindow(hwnd, memoryDc, PwRenderFullContent);
        if (!printed)
        {
          _ = BitBlt(memoryDc, 0, 0, width, height, windowDc, 0, 0, Srccopy);
        }

        var source = Imaging.CreateBitmapSourceFromHBitmap(
          bitmap,
          IntPtr.Zero,
          Int32Rect.Empty,
          BitmapSizeOptions.FromEmptyOptions());
        source.Freeze();

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(stream);
        return true;
      }
      finally
      {
        if (oldBitmap != IntPtr.Zero && memoryDc != IntPtr.Zero)
        {
          _ = SelectObject(memoryDc, oldBitmap);
        }

        if (bitmap != IntPtr.Zero)
        {
          _ = DeleteObject(bitmap);
        }

        if (memoryDc != IntPtr.Zero)
        {
          _ = DeleteDC(memoryDc);
        }

        _ = ReleaseDC(hwnd, windowDc);
      }
    }

    private static bool TrySaveRenderedWindow(Window window, string outputPath)
    {
      var width = (int)Math.Ceiling(window.ActualWidth);
      var height = (int)Math.Ceiling(window.ActualHeight);
      if (width <= 0 || height <= 0)
      {
        return false;
      }

      var source = PresentationSource.FromVisual(window);
      var dpiX = 96.0 * (source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0);
      var dpiY = 96.0 * (source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0);
      var bitmap = new RenderTargetBitmap(width, height, dpiX, dpiY, PixelFormats.Pbgra32);
      bitmap.Render(window);

      using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmap));
      encoder.Save(stream);
      return true;
    }

    private static Task WriteInfoAsync(CrashContext context, string message, CancellationToken cancellationToken)
    {
      return File.WriteAllTextAsync(
        Path.Combine(context.PackageDirectory, "screenshot-info.txt"),
        message,
        cancellationToken);
    }

    private static void WriteInfo(CrashContext context, string message)
    {
      File.WriteAllText(Path.Combine(context.PackageDirectory, "screenshot-info.txt"), message);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(
      IntPtr hdc,
      int x,
      int y,
      int cx,
      int cy,
      IntPtr hdcSrc,
      int x1,
      int y1,
      int rop);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }
  }
}

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace MainWindowProgram.Init
{
  internal static class ApplicationActivator
  {
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SwRestore = 9;
    private const string OpenFileCommandPrefix = "OPENFILE|";
    private static readonly ConcurrentQueue<string> _pendingFileRequests = new();

    public static void HandlePipeCommand(string? command)
    {
      if (string.IsNullOrWhiteSpace(command))
      {
        return;
      }

      if (string.Equals(command, "ACTIVATE", StringComparison.OrdinalIgnoreCase))
      {
        ActivateMainWindow();
        return;
      }

      if (!command.StartsWith(OpenFileCommandPrefix, StringComparison.Ordinal))
      {
        return;
      }

      var encodedPath = command[OpenFileCommandPrefix.Length..];
      if (!TryDecodeFilePath(encodedPath, out var filePath))
      {
        return;
      }

      RequestOpenFile(filePath);
    }

    public static void FlushPendingFileRequests()
    {
      while (_pendingFileRequests.TryDequeue(out var filePath))
      {
        if (!TryOpenFileInMainWindow(filePath))
        {
          _pendingFileRequests.Enqueue(filePath);
          break;
        }
      }
    }

    public static void ActivateMainWindow()
    {
      var dispatcher = Application.Current?.Dispatcher;
      if (dispatcher == null)
      {
        return;
      }

      dispatcher.BeginInvoke(() =>
      {
        var window = Application.Current?.MainWindow;
        if (window == null)
        {
          return;
        }

        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

        ShowWindow(handle, SwRestore);
        SetForegroundWindow(handle);
      });
    }

    private static bool TryDecodeFilePath(string encodedPath, out string filePath)
    {
      filePath = string.Empty;

      if (string.IsNullOrWhiteSpace(encodedPath))
      {
        return false;
      }

      try
      {
        var bytes = Convert.FromBase64String(encodedPath);
        var decoded = Encoding.UTF8.GetString(bytes);

        return SupportedFileExtensions.TryResolveSupportedExistingFile(decoded, out filePath);
      }
      catch
      {
        return false;
      }
    }

    private static void RequestOpenFile(string filePath)
    {
      if (!TryOpenFileInMainWindow(filePath))
      {
        _pendingFileRequests.Enqueue(filePath);
      }

      ActivateMainWindow();
    }

    private static bool TryOpenFileInMainWindow(string filePath)
    {
      var dispatcher = Application.Current?.Dispatcher;
      if (dispatcher == null)
      {
        return false;
      }

      var opened = false;

      dispatcher.Invoke(() =>
      {
        if (Application.Current?.MainWindow is MainWindowProgram.MainWindow mainWindow)
        {
          mainWindow.OpenFileFromExternalRequest(filePath);
          opened = true;
        }
      });

      return opened;
    }
  }
}

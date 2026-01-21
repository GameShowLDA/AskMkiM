using Photino.NET;
using System;
using System.Drawing;
using System.Threading;

namespace Ask.Support
{
  public sealed class HelpViewerWindow : IDisposable
  {
    private readonly object _sync = new();

    private Thread? _uiThread;
    private PhotinoWindow? _window;

    private volatile bool _disposeRequested;
    private volatile bool _isClosed;

    private string? _startUrl; // что открыть при старте

    public void Navigate(string url)
    {
      if (string.IsNullOrWhiteSpace(url))
        return;

      lock (_sync)
      {
        _startUrl = url;

        //Если окно уже запущено — безопаснее закрыть и поднять заново с новым StartUrl
        if (_window != null && !_isClosed)
        {
          try { _window.Close(); } catch { /* ignore */ }
          _window = null;
          _isClosed = true;
        }

        StartUiThreadIfNeeded();
      }
    }

    private void StartUiThreadIfNeeded()
    {
      if (_disposeRequested) return;
      //if (_uiThread != null && _uiThread.IsAlive) return;

      _isClosed = false;

      _uiThread = new Thread(UiThreadMain)
      {
        IsBackground = false, // важно для стабильности
        Name = "HelpViewer(Photino)"
      };
      _uiThread.SetApartmentState(ApartmentState.STA);
      _uiThread.Start();
    }

    private void UiThreadMain()
    {
      try
      {
        var url = _startUrl;

        var w = new PhotinoWindow()
          .SetTitle("Справочная система")
          .SetSize(new Size(1024, 768))
          .SetMinSize(600, 600);

        // КЛЮЧЕВОЕ: Photino требует StartUrl/StartString ДО WaitForClose()
        if (!string.IsNullOrWhiteSpace(url))
          w.StartUrl = url!;
        else
          w.StartString = "<html><body>Help</body></html>";

        // Делегат должен вернуть bool
        w.WindowClosing += (_, __) =>
        {
          _isClosed = true;
          return false;
        };

        lock (_sync) _window = w;

        w.WaitForClose();
      }
      catch
      {
        // тут можно логировать
      }
      finally
      {
        lock (_sync) _window = null;
        _isClosed = true;
      }
    }

    public void Dispose()
    {
      lock (_sync)
      {
        _disposeRequested = true;
        try { _window?.Close(); } catch { }
        _window = null;
        _isClosed = true;
        _uiThread = null;
      }
    }
  }
}
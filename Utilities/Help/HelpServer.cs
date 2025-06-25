using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Help
{
  public static class HelpServer
  {
    private static HttpListener _listener;
    private static int _port;
    private static CancellationTokenSource _cts;

    /// <summary>
    /// Запускает HTTP-сервер, если ещё не запущен.
    /// </summary>
    public static void EnsureStarted()
    {
      if (_listener != null) return;

      // Найти свободный порт
      var listener = new HttpListener();
      for (int p = 8000; p < 9000; p++)
      {
        try
        {
          listener.Prefixes.Add($"http://localhost:{p}/");
          listener.Start();
          _listener = listener;
          _port = p;
          break;
        }
        catch { listener.Prefixes.Clear(); }
      }

      if (_listener == null)
        throw new InvalidOperationException("Не удалось найти свободный порт для Help-сервера.");

      _cts = new CancellationTokenSource();
      Task.Run(() => ListenLoop(_cts.Token));
    }

    private static async Task ListenLoop(CancellationToken token)
    {
      var helpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "AppHelp");
      while (!token.IsCancellationRequested)
      {
        var ctx = await _listener.GetContextAsync();
        _ = Task.Run(() => ProcessRequest(ctx, helpDir), token);
      }
    }

    private static void ProcessRequest(HttpListenerContext ctx, string helpDir)
    {
      string urlPath = ctx.Request.Url.AbsolutePath.TrimStart('/');
      string local = Path.Combine(helpDir,
                                     urlPath.Replace('/', Path.DirectorySeparatorChar));
      if (Directory.Exists(local))
        local = Path.Combine(local, "index.html");

      if (!File.Exists(local))
      {
        ctx.Response.StatusCode = 404;
        using (var w404 = new StreamWriter(ctx.Response.OutputStream))
          w404.Write("404 Not Found");
        ctx.Response.Close();
        return;
      }

      // Определяем MIME
      string ext = Path.GetExtension(local).ToLowerInvariant();
      ctx.Response.ContentType = ext switch
      {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        _ => "application/octet-stream"
      };

      FileStream fs = null;
      try
      {
        fs = File.OpenRead(local);
        ctx.Response.ContentLength64 = fs.Length;
        // если браузер закроет сокет — мы просто перейдём в catch
        fs.CopyTo(ctx.Response.OutputStream);
      }
      catch (HttpListenerException hlex)
      {
        // клиент закрыл соединение: можно игнорировать
        Debug.WriteLine($"Client closed connection: {hlex.Message}");
      }
      catch (IOException ioex)
      {
        // любая файловая/сет ошибка — логируем, но не падаем серверу
        Debug.WriteLine($"I/O error while sending file: {ioex}");
      }
      finally
      {
        fs?.Dispose();
        try { ctx.Response.OutputStream.Close(); }
        catch { /* ничего */ }
        ctx.Response.Close();
      }
    }

    public static int Port => _port;
    public static void Stop()
    {
      _cts?.Cancel();
      _listener?.Stop();
      _listener = null;
    }
  }
}

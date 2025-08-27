using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities.Help
{
  public static class HelpServer
  {
    private static HttpListener _listener;
    private static int _port;
    private static CancellationTokenSource _cts;

    /// <summary>Базовый URL (становится валидным после EnsureStarted()).</summary>
    public static Uri BaseUrl => _port > 0 ? new Uri($"http://localhost:{_port}/") : null;

    /// <summary>
    /// Запускает HTTP-сервер на первом свободном порту, который выдаст ОС.
    /// </summary>
    public static void EnsureStarted()
    {
      if (_listener != null) return;

      const int maxAttempts = 10;
      Exception lastError = null;

      for (int attempt = 0; attempt < maxAttempts; attempt++)
      {
        int candidatePort = GetFreeTcpPort(); // спросили у ОС свободный порт

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{candidatePort}/");

        try
        {
          listener.Start();

          _listener = listener;
          _port = candidatePort;

          _cts = new CancellationTokenSource();
          _ = Task.Run(() => ListenLoop(_cts.Token));

          return; // успех
        }
        catch (HttpListenerException ex)
        {
          // Если отказ по правам (ERROR_ACCESS_DENIED = 5), повторять бессмысленно — пробрасываем.
          if (ex.ErrorCode == 5)
          {
            listener.Close();
            throw new InvalidOperationException(
              $"Нет прав на регистрацию префикса http://localhost:{candidatePort}/. " +
              $"Запустите от администратора или добавьте URLACL.", ex);
          }

          // Иначе попробуем ещё раз с другим портом (на случай гонки).
          lastError = ex;
          try { listener.Close(); } catch { /* ignore */ }
        }
        catch (Exception ex)
        {
          lastError = ex;
          try { listener.Close(); } catch { /* ignore */ }
        }
      }

      throw new InvalidOperationException(
        "Не удалось подобрать свободный порт для Help-сервера после нескольких попыток.", lastError);
    }

    private static int GetFreeTcpPort()
    {
      // Просим у ОС свободный порт из эпhemeral-диапазона.
      var l = new TcpListener(IPAddress.Loopback, 0);
      l.Start();
      int p = ((IPEndPoint)l.LocalEndpoint).Port;
      l.Stop();
      return p;
    }

    private static async Task ListenLoop(CancellationToken token)
    {
      var helpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "AppHelp");
      while (!token.IsCancellationRequested)
      {
        HttpListenerContext ctx = null;
        try
        {
          ctx = await _listener.GetContextAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException) { break; }
        catch (HttpListenerException) { break; }  // listener остановлен
        catch (Exception ex)
        {
          Debug.WriteLine($"Listener error: {ex}");
          continue;
        }

        _ = Task.Run(() => ProcessRequest(ctx, helpDir), token);
      }
    }

    private static void ProcessRequest(HttpListenerContext ctx, string helpDir)
    {
      string urlPath = ctx.Request.Url.AbsolutePath.TrimStart('/');

      // Нормализация пути и защита от выхода за корень
      string fullRoot = Path.GetFullPath(helpDir);
      string local = Path.GetFullPath(Path.Combine(fullRoot, urlPath.Replace('/', Path.DirectorySeparatorChar)));
      if (!local.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
      {
        ctx.Response.StatusCode = 403;
        SafeWrite(ctx, "403 Forbidden");
        return;
      }

      if (Directory.Exists(local))
        local = Path.Combine(local, "index.html");

      if (!File.Exists(local))
      {
        ctx.Response.StatusCode = 404;
        SafeWrite(ctx, "404 Not Found");
        return;
      }

      // MIME
      string ext = Path.GetExtension(local).ToLowerInvariant();
      ctx.Response.ContentType = ext switch
      {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream"
      };

      FileStream fs = null;
      try
      {
        fs = File.OpenRead(local);
        ctx.Response.ContentLength64 = fs.Length;
        fs.CopyTo(ctx.Response.OutputStream);
      }
      catch (HttpListenerException hlex)
      {
        Debug.WriteLine($"Client closed connection: {hlex.Message}");
      }
      catch (IOException ioex)
      {
        Debug.WriteLine($"I/O error while sending file: {ioex}");
      }
      finally
      {
        fs?.Dispose();
        try { ctx.Response.OutputStream.Close(); } catch { }
        try { ctx.Response.Close(); } catch { }
      }
    }

    private static void SafeWrite(HttpListenerContext ctx, string text)
    {
      try
      {
        using var w = new StreamWriter(ctx.Response.OutputStream, Encoding.UTF8, leaveOpen: true);
        w.Write(text);
      }
      catch { /* ignore */ }
      finally
      {
        try { ctx.Response.OutputStream.Close(); } catch { }
        try { ctx.Response.Close(); } catch { }
      }
    }

    public static int Port => _port;

    public static void Stop()
    {
      try { _cts?.Cancel(); } catch { }
      try { _listener?.Close(); } catch { }  // Close() надёжнее, чем Stop() + Dispose
      _listener = null;
      _cts = null;
      _port = 0;
    }
  }
}
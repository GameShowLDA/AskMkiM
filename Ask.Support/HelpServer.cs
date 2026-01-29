using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  /// <summary>
  /// Локальный HTTP-сервер для обслуживания HTML-контента справочной системы.
  /// Используется для отдачи файлов из каталога <c>AppHelp</c> через <see cref="HttpListener"/>.
  /// </summary>
  public static class HelpServer
  {
    /// <summary>
    /// Экземпляр <see cref="HttpListener"/>, обслуживающий входящие HTTP-запросы.
    /// Равен <c>null</c>, пока сервер не запущен.
    /// </summary>
    private static HttpListener _listener;

    /// <summary>
    /// TCP-порт, на котором запущен HTTP-сервер.
    /// Значение валидно только после вызова <see cref="EnsureStarted"/>.
    /// </summary>
    private static int _port;

    /// <summary>
    /// Источник токенов отмены для фонового цикла приёма соединений.
    /// Используется для корректной остановки сервера.
    /// </summary>
    private static CancellationTokenSource _cts;

    /// <summary>
    /// Базовый URL сервера (например, <c>http://localhost:54321/</c>).
    /// </summary>
    /// <remarks>
    /// Возвращает <c>null</c>, если сервер ещё не был запущен.
    /// </remarks>
    public static Uri BaseUrl =>
      _port > 0 ? new Uri($"http://localhost:{_port}/") : null;

    /// <summary>
    /// Текущий TCP-порт, на котором работает сервер.
    /// </summary>
    public static int Port => _port;

    /// <summary>
    /// Гарантирует запуск HTTP-сервера.
    /// Если сервер уже запущен, повторный вызов ничего не делает.
    /// </summary>
    /// <remarks>
    /// В случае нехватки прав на регистрацию URL — выбрасывает исключение.
    /// </remarks>
    public static void EnsureStarted()
    {
      if (_listener != null)
        return;

      int port = GetFreeTcpPort();

      var listener = new HttpListener();
      listener.Prefixes.Add($"http://localhost:{port}/");

      try
      {
        listener.Start();
      }
      catch (HttpListenerException ex) when (ex.ErrorCode == 5)
      {
        LogException($"Нет прав на регистрацию префикса http://localhost:{port}/...", ex);
        listener.Close();
        return;
      }
      catch (Exception ex)
      {
        LogException(ex);
        listener.Close();
        return;
      }

      _listener = listener;
      _port = port;

      LogInformation($"HTTP-сервер запущен.");

      _cts = new CancellationTokenSource();
      _ = Task.Run(() => ListenLoop(_cts.Token));
    }

    /// <summary>
    /// Запрашивает у операционной системы свободный TCP-порт
    /// из ephemeral-диапазона.
    /// </summary>
    /// <returns>Номер свободного TCP-порта.</returns>
    private static int GetFreeTcpPort()
    {
      var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      listener.Stop();

      LogInformation($"Выбран свободный TCP-порт {port} для HTTP-сервера.");

      return port;
    }

    /// <summary>
    /// Основной асинхронный цикл ожидания входящих HTTP-запросов.
    /// </summary>
    /// <param name="token">Токен отмены для корректной остановки сервера.</param>
    private static async Task ListenLoop(CancellationToken token)
    {
      var helpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppHelp");

      while (!token.IsCancellationRequested)
      {

        HttpListenerContext ctx;

        try
        {
          ctx = await _listener.GetContextAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex) when (token.IsCancellationRequested)
        {
          LogException(ex);
          break;
        }
        catch (HttpListenerException ex) when (token.IsCancellationRequested)
        {
          LogException(ex);
          break;
        }
        catch (Exception ex)
        {
          LogException(ex);
          continue;
        }

        _ = Task.Run(() => ProcessRequest(ctx, helpDir), token);
      }
    }

    /// <summary>
    /// Обрабатывает отдельный HTTP-запрос и отдаёт файл справочной системы.
    /// </summary>
    /// <param name="ctx">Контекст HTTP-запроса.</param>
    /// <param name="helpDir">Корневая директория с HTML-контентом.</param>
    private static void ProcessRequest(HttpListenerContext ctx, string helpDir)
    {
      string urlPath = ctx.Request.Url.AbsolutePath.TrimStart('/');

      string fullRoot = Path.GetFullPath(helpDir);
      string local = Path.GetFullPath(
        Path.Combine(fullRoot, urlPath.Replace('/', Path.DirectorySeparatorChar)));

      if (!local.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
      {
        ctx.Response.StatusCode = 403;
        SafeWrite(ctx, "403 Forbidden");
        LogError("Ошибка HTTP-запроса 403: доступ запрещён!");
        return;
      }

      if (Directory.Exists(local))
        local = Path.Combine(local, "index.html");

      if (!File.Exists(local))
      {
        ctx.Response.StatusCode = 404;
        SafeWrite(ctx, "404 Not Found");
        LogError("Ошибка HTTP-запроса 404: файл не найден!");
        return;
      }

      ctx.Response.ContentType = Path.GetExtension(local).ToLowerInvariant() switch
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

      using var fs = File.OpenRead(local);
      ctx.Response.ContentLength64 = fs.Length;
      fs.CopyTo(ctx.Response.OutputStream);

      ctx.Response.OutputStream.Close();
      ctx.Response.Close();
    }

    /// <summary>
    /// Безопасно записывает текстовый ответ клиенту
    /// (используется для ошибок 403 / 404).
    /// </summary>
    /// <param name="ctx">Контекст HTTP-запроса.</param>
    /// <param name="text">Текст ответа.</param>
    private static void SafeWrite(HttpListenerContext ctx, string text)
    {
      using var w = new StreamWriter(ctx.Response.OutputStream, Encoding.UTF8, leaveOpen: true);
      w.Write(text);
      ctx.Response.OutputStream.Close();
      ctx.Response.Close();
    }

    /// <summary>
    /// Останавливает HTTP-сервер и освобождает все ресурсы.
    /// </summary>
    public static void Stop()
    {
      _cts?.Cancel();
      _listener?.Close();
      _listener = null;
      _cts = null;
      _port = 0;
      LogInformation("HTTP-сервер остановлен.");
    }
  }
}
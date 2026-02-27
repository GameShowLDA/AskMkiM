using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  /// <summary>
  /// Локальный HTTP-сервер.
  /// </summary>
  public static class HelpServer
  {
    /// <summary>
    /// Интерфейс для управления жизненным циклом сервера.
    /// </summary>
    private static IHost? _host;

    /// <summary>
    /// Порт, на котором запущен сервер.
    /// </summary>
    private static int _port;

    /// <summary>
    /// Базовая URL-адрес сервера.
    /// </summary>
    public static Uri? BaseUrl => _port > 0 ? new Uri($"http://localhost:{_port}/") : null;

    /// <summary>
    /// Запускает сервер, если он ещё не запущен. Повторные вызовы безопасны.
    /// </summary>
    public static void EnsureStarted()
    {
      if (_host != null)
        return;

      var helpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppHelp");
      var fullHelpDir = Path.GetFullPath(helpDir);

      if (!Directory.Exists(fullHelpDir))
      {
        LogError($"Каталог справки не найден: {fullHelpDir}");
        return;
      }

      int port = GetFreeTcpPort();
      _port = port;

      _host = Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(webBuilder =>
        {
          webBuilder.UseKestrel(options =>
          {
            options.Listen(IPAddress.Loopback, port);
          });

          webBuilder.Configure(app =>
          {
            var provider = new PhysicalFileProvider(fullHelpDir);

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".woff"] = "font/woff";
            contentTypeProvider.Mappings[".woff2"] = "font/woff2";
            contentTypeProvider.Mappings[".ttf"] = "font/ttf";
            contentTypeProvider.Mappings[".webp"] = "image/webp";

            app.UseStaticFiles(new StaticFileOptions
            {
              FileProvider = provider,
              RequestPath = "",
              ContentTypeProvider = contentTypeProvider,
              ServeUnknownFileTypes = false
            });

            app.UseDefaultFiles(new DefaultFilesOptions
            {
              FileProvider = provider,
              RequestPath = ""
            });

            app.Run(async ctx =>
            {
              ctx.Response.StatusCode = StatusCodes.Status404NotFound;
              ctx.Response.ContentType = "text/plain; charset=utf-8";
              await ctx.Response.WriteAsync("404 Not Found");
            });
          });
        })
        .Build();

      _host.Start();

      LogInformation($"HelpServer (Kestrel) запущен: http://localhost:{_port}/  Root={fullHelpDir}");
    }

    /// <summary>
    /// Останавливает сервер. Штатная остановка происходит без исключений.
    /// </summary>
    public static void Stop()
    {
      _host?.Dispose();
      _host = null;
      LogInformation($"HelpServer (Kestrel) остановлен.");
    }

    /// <summary>
    /// Ищет и отдаёт первый попавшийся свободный порт.
    /// </summary>
    /// <returns>Номер свободного порта.</returns>
    private static int GetFreeTcpPort()
    {
      var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
      l.Start();
      int port = ((IPEndPoint)l.LocalEndpoint).Port;
      l.Stop();
      return port;
    }
  }
}
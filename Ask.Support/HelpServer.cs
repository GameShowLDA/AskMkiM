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
  /// Локальный HTTP-сервер, запускаемый при первом открытии справки.
  /// </summary>
  public static class HelpServer
  {
    private static readonly object SyncRoot = new();
    private static IHost? _host;
    private static PhysicalFileProvider? _files;
    private static Uri? _baseUrl;

    /// <summary>
    /// Адрес готового сервера, либо null до запуска/после остановки.
    /// </summary>
    public static Uri? BaseUrl
    {
      get { lock (SyncRoot) return _baseUrl; }
    }

    /// <summary>
    /// Запускает сервер, если он ещё не запущен. Повторные и параллельные вызовы безопасны.
    /// </summary>
    public static void EnsureStarted()
    {
      lock (SyncRoot)
      {
        if (_host != null) return;

        var helpDir = Path.Combine(AppContext.BaseDirectory, "AppHelp");
        if (!Directory.Exists(helpDir))
        {
          LogError($"Каталог справки не найден: {helpDir}");
          return;
        }

        // Для локальных файлов не нужны JSON-конфигурация, watchers, console logging и HTTPS.
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
          ApplicationName = typeof(HelpServer).Assembly.GetName().Name,
          ContentRootPath = helpDir,
          EnvironmentName = Environments.Production
        });
        // Kestrel резервирует порт сам, без гонки между поиском порта и запуском.
        builder.WebHost.UseKestrelCore().ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        var files = new PhysicalFileProvider(helpDir);
        IHost? host = null;
        try
        {
          var app = builder.Build();
          host = app;
          var contentTypes = new FileExtensionContentTypeProvider();
          contentTypes.Mappings[".woff"] = "font/woff";
          contentTypes.Mappings[".woff2"] = "font/woff2";
          contentTypes.Mappings[".ttf"] = "font/ttf";
          contentTypes.Mappings[".webp"] = "image/webp";

          app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
          app.UseStaticFiles(new StaticFileOptions
          {
            FileProvider = files,
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = false
          });
          app.Run(async context =>
          {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("404 Not Found");
          });

          host.Start();
          var address = new Uri(app.Urls.Single());
          // Сохраняем localhost как origin для существующих cookies/закладок.
          _baseUrl = new Uri($"http://localhost:{address.Port}/");
          _files = files;
          _host = host;
        }
        catch
        {
          host?.Dispose();
          files.Dispose();
          throw;
        }

        LogInformation($"HelpServer (Kestrel) запущен: {_baseUrl} Root={helpDir}");
      }
    }

    /// <summary>
    /// Останавливает сервер и освобождает файловый провайдер. Допускает повторный запуск.
    /// </summary>
    public static void Stop()
    {
      lock (SyncRoot)
      {
        try
        {
          _host?.Dispose();
        }
        finally
        {
          _host = null;
          _baseUrl = null;
          _files?.Dispose();
          _files = null;
        }
      }
    }
  }
}

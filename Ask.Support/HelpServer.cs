using System.IO;
using System.Net;
using Ask.Support.ReleaseNotes;
using Ask.Core.Services.Config.AppSettings;
using System.Text.Json;
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

          app.Use(async (context, next) =>
          {
            if (context.Request.Path == "/api/release-notes/viewed")
            {
              if (!HttpMethods.IsPost(context.Request.Method))
              {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
              }
              try
              {
                var request = await context.Request.ReadFromJsonAsync<ReleaseNotesViewedRequest>(context.RequestAborted);
                string? role = RoleAuthorizationConfig.CurrentRole?.ToString();
                if (request is null || role is null || request.Role != role)
                {
                  context.Response.StatusCode = StatusCodes.Status409Conflict;
                  return;
                }
                var releases = await new ReleaseNotesReader().ReadDirectoryAsync(
                  Path.Combine(helpDir, "releases"), context.RequestAborted);
                if (!releases.Any(release => release.Version == request.Version))
                {
                  context.Response.StatusCode = StatusCodes.Status400BadRequest;
                  return;
                }
                await Task.Run(() => ReleaseNotesReadStateStore.Default.MarkViewed(role, request.Version),
                  context.RequestAborted);
                context.Response.StatusCode = StatusCodes.Status204NoContent;
              }
              catch (Exception exception) when (exception is JsonException or InvalidDataException or IOException or UnauthorizedAccessException)
              {
                LogException(exception, customMessage: "Не удалось сохранить просмотр истории выпусков.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
              }
              return;
            }
            if (context.Request.Path != "/ru/Content/ReleaseNotes.html")
            {
              await next(context);
              return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            try
            {
              var releases = await new ReleaseNotesReader().ReadDirectoryAsync(
                Path.Combine(helpDir, "releases"), context.RequestAborted);
              await context.Response.WriteAsync(new ReleaseNotesHtmlRenderer().Render(
                releases, RoleAuthorizationConfig.CurrentRole?.ToString()),
                context.RequestAborted);
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
            {
              LogException(exception, customMessage: "Не удалось прочитать историю выпусков.");
              context.Response.StatusCode = StatusCodes.Status500InternalServerError;
              await context.Response.WriteAsync(
                "<!DOCTYPE html><html lang=\"ru\"><meta charset=\"utf-8\">" +
                "<link rel=\"stylesheet\" href=\"../../content.css\"><body><main class=\"page-content\">" +
                "<h1>Что нового</h1><p>Не удалось загрузить историю выпусков. " +
                "Попробуйте открыть раздел повторно. Подробности записаны в журнал.</p></main></body></html>",
                context.RequestAborted);
            }
          });

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

    private sealed record ReleaseNotesViewedRequest(string Role, string Version);
  }
}

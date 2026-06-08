using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Metrology;
using Ask.Core.Services.Usb;
using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.View;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Extensions;
using Ask.LogLib;
using Ask.Support;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Init
{
  /// <summary>
  /// Выполняет предварительную инициализацию приложения перед запуском основного окна.
  /// </summary>
  /// <remarks>
  /// Класс <see cref="PreStartupInitializer"/> отвечает за выполнение всех необходимых действий,
  /// которые должны быть завершены до отображения главного окна приложения.
  /// 
  /// В частности, при вызове метода <see cref="Initialize"/> выполняются следующие шаги:
  /// <list type="number">
  /// <item>
  /// <description>
  /// Проверка наличия уже запущенного экземпляра приложения с помощью 
  /// <see cref="SingleInstanceManager.EnsureSingleInstance"/>.  
  /// Если экземпляр уже существует, новый запуск будет предотвращён.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Инициализация и проверка базы данных с помощью 
  /// <see cref="DatabaseInitializer.InitializeAsync"/> — включая подготовку подключения,
  /// создание структуры при необходимости и загрузку исходных данных.
  /// </description>
  /// </item>
  /// </list>
  /// 
  /// После успешного выполнения всех этапов приложение готово к запуску основной логики
  /// и отображению пользовательского интерфейса.
  /// </remarks>
  internal class PreStartupInitializer
  {
    private static int _loggedExceptionHookRegistered;

    public static IHost AppHost { get; private set; }

    /// <summary>
    /// Запускает процедуру предварительной инициализации приложения.
    /// </summary>
    /// <remarks>
    /// Выполняет проверку единственного экземпляра приложения и инициализацию базы данных.
    /// Этот метод должен вызываться один раз — до загрузки основного окна.
    /// </remarks>
    static internal async Task Initialize()
    {
      // SingleInstanceManager.EnsureSingleInstance();
      await DatabaseInitializer.InitializeAsync();
      InitializeAppHost();
      InitializeHelpServer();
    }

    /// <summary>
    /// Выполняет инициализацию DI-контейнера и регистрацию основных служб приложения.
    /// </summary>
    private static void InitializeAppHost()
    {
      AppHost = Host.CreateDefaultBuilder()
          .ConfigureServices(services =>
          {
            services.AddSingleton<Dispatcher>(_ => Application.Current.Dispatcher);

            services.AddSingleton<IUsbMonitorView, UsbMonitorService>();
            services.AddSingleton<MetrologyControlFactory>();

            services.AddCrashDiagnostics(
              options =>
              {
                options.Path = Path.Combine(AppContext.BaseDirectory, "CrashReports");
                options.MaxRetainedReports = 30;
                options.IncludeScreenshot = true;
                options.IncludeLogs = true;
                options.IncludeConfig = true;
                options.AutoZip = false;
                options.CreatePackageForLoggedExceptions = true;
                options.LoggedExceptionThrottleWindow = TimeSpan.FromMinutes(2);
                options.LoggedExceptionReportTimeout = TimeSpan.FromSeconds(30);
                options.MaxPendingLoggedExceptionReports = 2;
                options.CommandHistoryCapacity = 500;
                options.MaxLogBytes = 5 * 1024 * 1024;
                options.LogFilePaths.Add(Path.Combine(AppContext.BaseDirectory, "logs"));
                options.ConfigFilePaths.Add(Path.Combine(AppContext.BaseDirectory, "Settings"));
              },
              message => LogInformation(message),
              (exception, message) => LogException(exception, customMessage: message),
              ShowCrashPackageCreatedNotification);

            services.AddDiagnosticStateProvider("Application", () => new
            {
              mainWindow = Application.Current?.MainWindow?.GetType().FullName,
              dispatcherThreadId = Application.Current?.Dispatcher?.Thread.ManagedThreadId,
              baseDirectory = AppContext.BaseDirectory,
              currentDirectory = Environment.CurrentDirectory,
            });

            services.AddDiagnosticConfigProvider("AppSettings", async (_, _) => new
            {
              execution = await ExecutionConfig.GetExecitonModel(),
              protocol = ProtocolConfig.GetProtocolModel(),
            });

            RegisterMetrologyControls(services);
          })
          .Build();

      ServiceLocator.Initialize(AppHost);
      AppHost.StartAsync().GetAwaiter().GetResult();
      RegisterLoggedExceptionCrashReporting();
      _ = Task.Run(() => InitializeChassisDevices());
    }

    private static void RegisterLoggedExceptionCrashReporting()
    {
      if (Interlocked.Exchange(ref _loggedExceptionHookRegistered, 1) == 1)
      {
        return;
      }

      LoggerUtility.ExceptionLoggedCallback = args => LoggerUtility_ExceptionLogged(null, args);
      LoggerUtility.ExceptionLogged += LoggerUtility_ExceptionLogged;
      LogInformation($"Crash diagnostics logger hook registered. Reports path: {Path.Combine(AppContext.BaseDirectory, "CrashReports")}");
    }

    private static void LoggerUtility_ExceptionLogged(object? sender, LoggedExceptionEventArgs e)
    {
      try
      {
        var reporter = AppHost?.Services.GetService<IExceptionDiagnosticReporter>();
        if (reporter == null)
        {
          return;
        }

        reporter.Report(e.Exception, BuildLoggedExceptionSource(e));
      }
      catch
      {
      }
    }

    private static string BuildLoggedExceptionSource(LoggedExceptionEventArgs e)
    {
      var file = string.IsNullOrWhiteSpace(e.CallerFilePath)
        ? "unknown"
        : Path.GetFileName(e.CallerFilePath);

      return string.IsNullOrWhiteSpace(e.CustomMessage)
        ? $"{file}:{e.LineNumber}"
        : $"{e.CustomMessage} ({file}:{e.LineNumber})";
    }

    private static void ShowCrashPackageCreatedNotification(string path)
    {
      NotificationHostService.Instance.Show(
        "Диагностика",
        $"Отчёт об ошибке сохранён:{Environment.NewLine}{path}",
        NotificationType.Warning);
    }

    /// <summary>
    /// Выполняет первичную инициализацию устройств, связанных с первым найденным шасси.
    /// </summary>
    private static void InitializeChassisDevices()
    {
      try
      {
        LogInformation("Инициализация устройств шасси: начало");

        var chassis = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
        if (chassis == null)
        {
          LogInformation("Инициализация устройств шасси: шасси не найдено");
          return;
        }
        var tester = BreakdownTesters.GetDevicesByNumberChassisAsync(chassis.Number).GetAwaiter().GetResult().FirstOrDefault();

        LogInformation($"Инициализация устройств шасси завершена для №{chassis.Number}");
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    private static void RegisterMetrologyControls(IServiceCollection services)
    {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();

      var controls = assemblies
          .SelectMany(a =>
          {
            try { return a.GetTypes(); }
            catch { return Array.Empty<Type>(); }
          })
          .Where(t => !t.IsAbstract && typeof(UserControl).IsAssignableFrom(t))
          .Select(t => new
          {
            Type = t,
            Attr = t.GetCustomAttribute<MetrologyModeAttribute>()
          })
          .Where(x => x.Attr != null);

      foreach (var c in controls)
        services.AddTransient(c.Type);
    }

    private static void InitializeHelpServer()
    {
      try
      {
        HelpServer.EnsureStarted();
      }
      catch (Exception ex)
      {
        LogException(ex: ex, customMessage: $"Не удалось запустить Help-сервер.", file: "Utilities\\Help\\HelpServer.cs");
      }
    }
  }
}

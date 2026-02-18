using Ask.Core.Services.App;
using Ask.Core.Services.Metrology;
using Ask.Core.Services.Usb;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Ask.Core.Shared.Metadata.View;
using Ask.Support;
using DataBaseConfiguration.Services.Device;
using MainWindowProgram.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewCore.Device;
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

            services.AddSingleton<IBreakdownTester, GPT79904>();
            services.AddSingleton<BreakdownTesterServices>();

            services.AddSingleton<IUsbMonitorView, UsbMonitorService>();
            services.AddSingleton<MetrologyControlFactory>();

            RegisterMetrologyControls(services);
          })
          .Build();

      ServiceLocator.Initialize(AppHost);
      AppHost.StartAsync().GetAwaiter().GetResult();
      _ = Task.Run(() => InitializeChassisDevices());
    }

    /// <summary>
    /// Выполняет первичную инициализацию устройств, связанных с первым найденным шасси.
    /// </summary>
    private static void InitializeChassisDevices()
    {
      try
      {
        LogInformation("Инициализация устройств шасси: начало");

        var chassis = new ChassisManagerServices().GetAll().FirstOrDefault();
        if (chassis == null)
        {
          LogInformation("Инициализация устройств шасси: шасси не найдено");
          return;
        }

        var testerService = ServiceLocator.GetRequired<BreakdownTesterServices>();
        var tester = testerService.GetDevicesByNumberChassis(chassis.Number).FirstOrDefault();

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

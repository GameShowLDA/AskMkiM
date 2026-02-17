using Ask.Core.Services.App;
using Ask.Core.Services.Usb;
using Ask.Core.Shared.Metadata.View;
using Ask.Support;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace MainWindowProgram.Engine
{
  /// <summary>
  /// Регистрирует и инициализирует все основные сервисы приложения.
  /// </summary>
  internal static class AppServices
  {
    /// <summary>
    /// Строит и возвращает ViewModel и USB-сервис для MainWindow.
    /// </summary>
    /// <param name="window">Главное окно приложения.</param>
    /// <returns>Кортеж из ViewModel и UsbServices.</returns>
    public static (MainWindowViewModel viewModel, IUsbMonitorView usb) Build(MainWindow window)
    {
      var multi = new MultiWindowService(window.MultiWindow);

      var usb = ServiceLocator.GetRequired<IUsbMonitorView>();
      var file = new FileService(window, multi, () => window.IsLocked);

      // TODO : Как толкьо разберусь с MultiWindowService, надо будет пихнуть в синглтон. Интерфейс IMetrologyServiceView
      var metrology = ActivatorUtilities.CreateInstance<MetrologyService>(ServiceLocator.Services, multi);

      var admin = new AdminServices(window, multi);
      var test = new TestService(multi);
      var settings = new SettingsService(multi);
      var windowService = new WindowService(window, window.mainMenu, window.ButtonsPanel, () => window.IsLocked);
      var selfTest = new SelfTestServices(multi);
      var translation = new TranslationServices(multi, file);
      var run = new RunServices(multi, file);

      var viewModel = new MainWindowViewModel(
          metrology,
          file,
          test,
          settings,
          admin,
          windowService,
          selfTest,
          translation,
          run
      );

      HelpProvider.RegisterHelp(window);

      return (viewModel, usb);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;
using Utilities.Help;

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
    public static (MainWindowViewModel viewModel, UsbServices usb) Build(MainWindow window)
    {
      var multi = new MultiWindowService(window.MultiWindow);
      var usb = new UsbServices();
      var file = new FileService(window, multi, () => window.IsLocked);
      var metrology = new MetrologyService(multi);
      var admin = new AdminServices(window, multi);
      var test = new TestService(multi);
      var settings = new SettingsService(multi);
      var windowService = new WindowService(window, window.mainMenu, window.ButtonsPanel, () => window.IsLocked);

      var viewModel = new MainWindowViewModel(
          metrology,
          file,
          test,
          settings,
          admin,
          windowService
      );

      HelpProvider.RegisterHelp(window);
      return (viewModel, usb);
    }
  }
}

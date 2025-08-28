using ConsoleUI.ConsoleCommanding.Services;
using MainWindowProgram.Events;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainWindowProgram.Engine
{
  /// <summary>
  /// Управляет привязкой событий жизненного цикла приложения.
  /// </summary>
  internal class ApplicationLifecycleManager
  {
    static internal ApplicationEventsBinder ApplicationEvents;

    /// <summary>
    /// Инициализирует события приложения и связывает их с соответствующими биндерами.
    /// </summary>
    /// <param name="window">Главное окно приложения.</param>
    /// <param name="usb">Сервис USB.</param>
    /// <param name="console">Консольный менеджер.</param>
    public void Initialize(MainWindow window, UsbServices usb, TextEditorStatusViewModel statusBarViewModel)
    {
      ApplicationEvents = new ApplicationEventsBinder(
          new SystemEventsBinder(),
          new UiEventsBinder(window, window.MultiWindow, statusBarViewModel),
          new StateEventsBinder(window, usb)
      );

      ApplicationEvents.BindAll();
    }
  }
}

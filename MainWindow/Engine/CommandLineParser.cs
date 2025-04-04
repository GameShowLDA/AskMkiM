using System;
using System.Linq;
using MainWindowProgram.Services;

namespace MainWindowProgram.Engine
{
  /// <summary>
  /// Обрабатывает аргументы командной строки и настраивает поведение приложения.
  /// </summary>
  internal class CommandLineParser
  {
    private readonly UsbServices _usbServices;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CommandLineParser"/>.
    /// </summary>
    /// <param name="usbServices">Сервис управления USB-мониторингом.</param>
    public CommandLineParser(UsbServices usbServices)
    {
      _usbServices = usbServices ?? throw new ArgumentNullException(nameof(usbServices));
    }

    /// <summary>
    /// Запускает обработку аргументов командной строки.
    /// Сначала применяются значения по умолчанию, затем каждый аргумент обрабатывается отдельно.
    /// </summary>
    internal void ProcessCommandLineArgs()
    {
      ResetDefaults();

      string[] args = App.CommandLineArgs;

      // Обработка специфических аргументов
      if (!args.Contains("admin"))
      {
        _usbServices.SetUsbMonitoring(false);
      }
      else
      {
        _usbServices.SetUsbMonitoring(true);
      }

      foreach (var arg in args.Select(a => a.ToLowerInvariant()))
      {
        switch (arg)
        {
          case "admin":
            HandleAdminMode();
            break;

          default:
            HandleUnknownArgument(arg);
            break;
        }
      }
    }

    /// <summary>
    /// Обрабатывает включение режима администратора.
    /// Включает USB-мониторинг.
    /// </summary>
    private void HandleAdminMode()
    {
      _usbServices.SetUsbMonitoring(true);
    }

    /// <summary>
    /// Обрабатывает неизвестные аргументы командной строки.
    /// Может использоваться для логирования или отладки.
    /// </summary>
    /// <param name="arg">Неизвестный аргумент.</param>
    private void HandleUnknownArgument(string arg)
    {
      Console.WriteLine($"[Warning] Неизвестный аргумент: {arg}");
    }

    /// <summary>
    /// Устанавливает значения по умолчанию перед обработкой аргументов.
    /// Сбрасывает настройки в стартовое состояние.
    /// </summary>
    private void ResetDefaults()
    {
      _usbServices.SetUsbMonitoring(false);
    }
  }
}

using Ask.Core.Services.Config.AppSettings;
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

      bool isAdmin = false;

      foreach (var raw in App.CommandLineArgs)
      {
        var token = raw.TrimStart('-', '/');

        if (token.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
          isAdmin = true;
          HandleAdminMode();
        }
        else if (token.Equals("debug", StringComparison.OrdinalIgnoreCase))
        {
          AdminConfig.SetDebugRights(true).ConfigureAwait(false);
        }
        else
        {
          HandleUnknownArgument(raw);
        }
      }

      _usbServices.SetUsbMonitoring(isAdmin);
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

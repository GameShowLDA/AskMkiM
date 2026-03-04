using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Metadata.View;
using MainWindowProgram.Init;

namespace MainWindowProgram.Engine
{
  /// <summary>
  /// Обрабатывает аргументы командной строки и настраивает поведение приложения.
  /// </summary>
  internal class CommandLineParser
  {
    private readonly IUsbMonitorView _usbServices;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CommandLineParser"/>.
    /// </summary>
    /// <param name="usbServices">Сервис управления USB-мониторингом.</param>
    public CommandLineParser(IUsbMonitorView usbServices)
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
      var filesToOpen = new List<string>();
      var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var raw in App.CommandLineArgs)
      {
        if (IsSwitch(raw, "admin"))
        {
          isAdmin = true;
          HandleAdminMode();
        }
        else if (IsSwitch(raw, "debug"))
        {
          AdminConfig.SetDebugRights(true).ConfigureAwait(false);
        }
        else if (SupportedFileExtensions.TryResolveSupportedExistingFile(raw, out var filePath))
        {
          if (seenPaths.Add(filePath))
          {
            filesToOpen.Add(filePath);
          }
        }
        else
        {
          HandleUnknownArgument(raw);
        }
      }

      _usbServices.SetUsbMonitoring(isAdmin);
      OpenRequestedFiles(filesToOpen);
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

    private static bool IsSwitch(string rawArg, string switchName)
    {
      if (string.IsNullOrWhiteSpace(rawArg))
      {
        return false;
      }

      var token = rawArg.Trim().TrimStart('-', '/');
      return token.Equals(switchName, StringComparison.OrdinalIgnoreCase);
    }

    private static void OpenRequestedFiles(IEnumerable<string> filesToOpen)
    {
      foreach (var filePath in filesToOpen)
      {
        FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(filePath);
      }
    }
  }
}

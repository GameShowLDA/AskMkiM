using Photino.NET;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  /// <summary>
  /// Управление обёрткой над окном Photino.NET для отображения справочной системы.
  /// </summary>
  public static class HelpViewerWindow
  {
    /// <summary>
    /// Экземпляр окна справки. Пока окно открыто — не <see langword="null"/>.
    /// </summary>
    private static PhotinoWindow? _helpWindow;

    /// <summary>
    /// Признак состояния окна: <see langword="true"/> — окно закрыто,
    /// <see langword="false"/> — окно считается открытым.
    /// </summary>
    /// <remarks>
    /// Флаг используется, чтобы не входить повторно в <see cref="PhotinoWindow.WaitForClose"/>
    /// и не блокировать логику открытия при повторных вызовах.
    /// </remarks>
    public static bool _IsClose { get; private set; } = true;

    /// <summary>
    /// Загружает страницу справочной системы по относительному адресу.
    /// Также поднимает окно поверх основного приложения и фокусирует его.
    /// </summary>
    /// <param name="page">Относительный путь страницы.</param>
    /// <remarks>
    /// Если окно ещё не создано (<see cref="_helpWindow"/> равен <see langword="null"/>), метод ничего не сделает.
    /// Для типового сценария используй <see cref="LoadAndShow"/>.
    /// </remarks>
    public static void Load(string page)
    {
      _helpWindow?.Load($"http://localhost:{HelpServer.Port}" + page);
      _helpWindow?.SetTopMost(true);
      _helpWindow?.SetTopMost(false);
    }

    /// <summary>
    /// Запускает окно и блокируется до его закрытия пользователем.
    /// </summary>
    public static void Show() => _helpWindow?.WaitForClose();

    /// <summary>
    /// Программно закрывает окно справки и освобождает ресурсы.
    /// </summary>
    public static void Close()
    {
      _helpWindow?.Close();
      _helpWindow = null;
      _IsClose = true;
      LogInformation("Окно справки закрыто.");
    }

    /// <summary>
    /// Применяет настройки к окну справки.
    /// </summary>
    /// <remarks>
    /// Метод предполагает, что <see cref="_helpWindow"/> уже создано и не равно <see langword="null"/>.
    /// </remarks>
    private static void SetSettings()
    {
      _helpWindow
        .SetTitle("Справочная система")
        .SetUseOsDefaultLocation(true)
        .SetMinSize(800, 600)
        .SetSize(1600, 900) //TODO: Почему-то данный параметр игнорируется? Надо узнать, почему!
        .RegisterWebMessageReceivedHandler((sender, message) =>
        {
          LogDebug(
            $"A JavaScript message from the HELP-system:\n" +
            $"Object: {sender}\n" +
            $"Message: {message}"
            );
        }
        )
        .Center();

      _helpWindow.RegisterWindowClosingHandler((sender, e) =>
      {
        _IsClose = true;
        _helpWindow = null;
        return false;
      });

      LogInformation("Настройки окна справки применены.");
    }

    /// <summary>
    /// Подготавливает и откроывает окно помощи.
    /// </summary>
    /// <param name="page">Относительный адрес страницы справки.</param>
    /// <remarks>
    /// Если окно уже было открыто, метод просто выполнит <see cref="Load"/> (то есть переключит страницу),
    /// а повторно в <see cref="Show"/> не войдёт.
    /// </remarks>
    public static void LoadAndShow(string page)
    {
      if (_helpWindow == null)
      {
        _helpWindow = new PhotinoWindow();
        SetSettings();
        _IsClose = true;
        LogInformation("Инициализирован экземпляр окна справки.");
      }

      Load(page);

      if (!_IsClose) return;
      try
      {
        _IsClose = false;
        Show();
      }
      catch (Exception ex)
      {
        LogError($"Ошмбка в HelpViewerWindow\n{ex}");
      }
    }
  }
}
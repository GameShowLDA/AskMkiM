using System.Windows.Input;

namespace Ask.UI.Features.ProtocolNew.Hotkeys
{
  /// <summary>
  /// Предоставляет контроллеру горячих клавиш состояние исполнительного интерфейса
  /// и операции, которые разрешено вызвать с клавиатуры.
  /// </summary>
  internal interface IProtocolHotkeyContext
  {
    /// <summary>Возвращает признак доступности запуска.</summary>
    bool CanStart { get; }

    /// <summary>Возвращает признак доступности паузы.</summary>
    bool CanPause { get; }

    /// <summary>Возвращает признак доступности продолжения.</summary>
    bool CanContinue { get; }

    /// <summary>Возвращает признак доступности завершения.</summary>
    bool CanExit { get; }

    /// <summary>Возвращает признак доступности повтора.</summary>
    bool CanRepeat { get; }

    /// <summary>Запускает выполнение.</summary>
    void Start();

    /// <summary>Выполняет действие F5 для текущего состояния.</summary>
    void RunOrPause();

    /// <summary>Запускает или продолжает выполнение в пошаговом режиме.</summary>
    /// <param name="isStepInto"><c>true</c> — шаг вглубь; <c>false</c> — шаг поверх.</param>
    void Step(bool isStepInto);

    /// <summary>Приостанавливает выполнение.</summary>
    void Pause();

    /// <summary>Продолжает выполнение.</summary>
    void Continue();

    /// <summary>Завершает выполнение.</summary>
    void Exit();

    /// <summary>Повторяет текущую операцию.</summary>
    void Repeat();

    /// <summary>Передаёт необработанную клавишу существующим подписчикам.</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы нажатия клавиши.</param>
    void NotifyOtherKey(object sender, KeyEventArgs e);
  }
}

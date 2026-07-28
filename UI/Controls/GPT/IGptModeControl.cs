using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace UI.Controls.GPT
{
  /// <summary>
  /// Определяет управление состоянием режима GPT в административном интерфейсе.
  /// </summary>
  internal interface IGptModeControl
  {
    /// <summary>
    /// Тип режима пробойной установки.
    /// </summary>
    BreakdownTypeMode ModeType { get; }

    /// <summary>
    /// Признак активного режима в интерфейсе.
    /// </summary>
    bool IsModeActive { get; }

    /// <summary>
    /// Переключает реальное устройство в режим и активирует его интерфейс.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если режим успешно активирован.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    Task<bool> ActivateModeAsync();

    /// <summary>
    /// Сбрасывает визуальное состояние режима без отправки команды устройству.
    /// </summary>
    void DeactivateMode();
  }
}

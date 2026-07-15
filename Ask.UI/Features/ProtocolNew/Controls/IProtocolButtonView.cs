using System.Windows;

namespace Ask.UI.Features.ProtocolNew.Controls;

/// <summary>
/// Предоставляет контроллеру кнопок минимальный доступ к видимости элементов <c>ProtocolUI</c>.
/// </summary>
internal interface IProtocolButtonView
{
  /// <summary>
  /// Видимость кнопки запуска.
  /// </summary>
  Visibility StartVisibility { get; set; }

  /// <summary>
  /// Видимость кнопки паузы.
  /// </summary>
  Visibility PauseVisibility { get; set; }

  /// <summary>
  /// Видимость кнопки продолжения.
  /// </summary>
  Visibility ContinueVisibility { get; set; }

  /// <summary>
  /// Видимость кнопки завершения.
  /// </summary>
  Visibility ExitVisibility { get; set; }

  /// <summary>
  /// Видимость кнопки повторного выполнения.
  /// </summary>
  Visibility RepeatVisibility { get; set; }

  /// <summary>
  /// Видимость кнопки циклического выполнения.
  /// </summary>
  Visibility LoopVisibility { get; set; }

  /// <summary>
  /// Видимость кнопки шага поверх.
  /// </summary>
  Visibility StepOverVisibility { get; set; }

  /// <summary>
  /// Видимость кнопки шага внутрь.
  /// </summary>
  Visibility StepIntoVisibility { get; set; }
}

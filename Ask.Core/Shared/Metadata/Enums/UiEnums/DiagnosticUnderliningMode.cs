namespace Ask.Core.Shared.Metadata.Enums.UiEnums;

/// <summary>Режим отображения диагностических подчёркиваний в редакторе.</summary>
public enum DiagnosticUnderliningMode
{
  /// <summary>Не показывать диагностические подчёркивания.</summary>
  None,

  /// <summary>Показывать ошибки и стилистические предупреждения.</summary>
  All,

  /// <summary>Показывать только ошибки.</summary>
  ErrorsOnly
}

/// <summary>Преобразование режима в совместимые флаги конфигурации.</summary>
public static class DiagnosticUnderliningModeExtensions
{
  /// <summary>Преобразует сохранённые флаги в режим выбора.</summary>
  public static DiagnosticUnderliningMode FromVisibility(bool showErrors, bool showWarnings) =>
    showWarnings
      ? DiagnosticUnderliningMode.All
      : showErrors
        ? DiagnosticUnderliningMode.ErrorsOnly
        : DiagnosticUnderliningMode.None;

  /// <summary>Определяет, показывает ли режим ошибки.</summary>
  public static bool ShowsErrors(this DiagnosticUnderliningMode mode) =>
    mode is DiagnosticUnderliningMode.All or DiagnosticUnderliningMode.ErrorsOnly;

  /// <summary>Определяет, показывает ли режим стилистические предупреждения.</summary>
  public static bool ShowsWarnings(this DiagnosticUnderliningMode mode) =>
    mode == DiagnosticUnderliningMode.All;
}

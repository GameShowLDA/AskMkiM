namespace Ask.Engine.ControlCommandAnalyser;

/// <summary>
/// Описывает диагностический диапазон в неизменённом снимке редактора со смещениями UTF-16.
/// </summary>
/// <param name="Offset">Начальное смещение диагностического диапазона.</param>
/// <param name="Length">Длина диагностического диапазона.</param>
/// <param name="LineNumber">Номер исходной строки.</param>
/// <param name="IsWarning">Признак предупреждения вместо ошибки.</param>
/// <param name="Description">Описание найденной проблемы.</param>
/// <param name="Code">Код диагностического сообщения.</param>
/// <param name="Suggestion">Предлагаемое изменение исходного текста.</param>
public sealed record SourceDiagnostic(
  int Offset,
  int Length,
  int LineNumber,
  bool IsWarning,
  string Description,
  string? Code,
  SourceTextSuggestion? Suggestion = null);

/// <summary>
/// Описывает предлагаемое изменение исходного текста для диагностического предупреждения.
/// </summary>
/// <param name="Offset">Смещение, в которое нужно вставить текст.</param>
/// <param name="Text">Текст, вставляемый в документ.</param>
/// <param name="PreviewText">Текст, отображаемый как полупрозрачная подсказка.</param>
/// <param name="PreviewOnNextLine">Признак отображения подсказки на виртуальной следующей строке.</param>
/// <param name="ReplacementLength">Длина заменяемого фрагмента; ноль означает обычную вставку.</param>
/// <param name="PreviewOffset">
/// Отдельное смещение полупрозрачной подсказки или <see langword="null"/> для места правки.
/// </param>
public sealed record SourceTextSuggestion(
  int Offset,
  string Text,
  string PreviewText,
  bool PreviewOnNextLine = false,
  int ReplacementLength = 0,
  int? PreviewOffset = null);

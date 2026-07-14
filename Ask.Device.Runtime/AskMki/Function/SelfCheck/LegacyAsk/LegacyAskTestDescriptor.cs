namespace Ask.Engine.Tests.LegacyAsk;

/// <summary>
/// Описывает пункт меню перенесенного теста старой программы АСК.
/// </summary>
/// <param name="Kind">Группа тестов.</param>
/// <param name="Code">Код теста из старой программы MKI.</param>
/// <param name="Title">Название теста для новой оболочки.</param>
/// <param name="Description">Краткое описание назначения теста.</param>
/// <param name="RequiredDevice">Оборудование, без которого тест нельзя запускать.</param>
public sealed record LegacyAskTestDescriptor(
  LegacyAskTestKind Kind,
  string Code,
  string Title,
  string Description,
  LegacyAskRequiredDevice RequiredDevice);

/// <summary>
/// Описывает оборудование, наличие которого требуется для выполнения теста.
/// </summary>
public enum LegacyAskRequiredDevice
{
  /// <summary>Контроллер стойки старой АСК.</summary>
  Controller,

  /// <summary>Цифровой вольтметр.</summary>
  Voltmeter,

  /// <summary>Аналого-цифровой преобразователь.</summary>
  Adc,

  /// <summary>ПИНТ3 или ПИНТ4.</summary>
  Pint,

  /// <summary>ПИНТ4.</summary>
  Pint4,

  /// <summary>Пробойная установка.</summary>
  Ppu,

  /// <summary>Прибор контроля изоляции.</summary>
  Pki,

  /// <summary>Измеритель емкости.</summary>
  LcMeter,

  /// <summary>Таймер контроллера.</summary>
  Timer,

  /// <summary>Коммутатор стойки.</summary>
  Commutator
}

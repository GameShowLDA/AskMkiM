namespace Ask.Engine.Tests.LegacyAsk;

/// <summary>
/// Группа legacy-тестов старой программы АСК.
/// </summary>
public enum LegacyAskTestKind
{
  /// <summary>Погрешности измерения из меню Prec.</summary>
  MeasurementAccuracy,

  /// <summary>Дополнительные сервисные тесты из меню Serv.</summary>
  AdditionalService,

  /// <summary>Тренировка реле из меню Relay.</summary>
  RelayTraining,

  /// <summary>Измерение времени срабатывания из меню Time.</summary>
  SwitchingTime
}

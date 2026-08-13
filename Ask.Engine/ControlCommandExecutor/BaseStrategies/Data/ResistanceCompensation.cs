namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;

/// <summary>
/// Корректирует измеренное сопротивление с учётом сопротивления коммутации.
/// </summary>
internal static class ResistanceCompensation
{
  /// <summary>
  /// Вычитает сопротивление коммутации и ограничивает результат снизу значением 0 Ом.
  /// </summary>
  /// <param name="measuredResistance">Измеренное сопротивление, Ом.</param>
  /// <param name="switchResistance">Сопротивление коммутации, Ом.</param>
  /// <param name="subtract">
  /// <see langword="true"/>, если требуется вычесть сопротивление коммутации;
  /// в противном случае — <see langword="false"/>.
  /// </param>
  /// <returns>Скорректированное сопротивление, не меньше 0 Ом.</returns>
  internal static double SubtractSwitchResistance(double measuredResistance, double switchResistance, bool subtract)
  {
    var compensatedResistance = subtract
      ? measuredResistance - switchResistance
      : measuredResistance;

    return Math.Max(0, compensatedResistance);
  }
}

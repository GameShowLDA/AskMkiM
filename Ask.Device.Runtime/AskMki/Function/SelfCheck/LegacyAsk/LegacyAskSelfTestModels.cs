namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Пара шин для проверки коммутации устройств.
/// </summary>
public readonly record struct LegacyAskBusPair(ushort Positive, ushort Negative, string PositiveName, string NegativeName);

/// <summary>
/// Диапазон стоек и БК для проверки коммутатора.
/// </summary>
public readonly record struct LegacyAskSwitchRange(int Stand, string Name, int FirstBk, int LastBk);

/// <summary>
/// Ожидаемое значение измерения АЦП.
/// </summary>
public readonly record struct LegacyAskExpectedValue(string RangeText, double Value, double Tolerance, string ExpectedText, bool MustBeOverload);

/// <summary>
/// Ожидаемое сопротивление для проверки АЦП.
/// </summary>
public readonly record struct LegacyAskResistanceCase(string RangeText, double ValueOhm, double ToleranceOhm, bool MustBeOverload);

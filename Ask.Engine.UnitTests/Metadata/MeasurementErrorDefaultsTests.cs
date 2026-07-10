using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;

namespace Ask.Engine.UnitTests.Metadata;

/// <summary>
/// Содержит модульные тесты для проверки значений погрешностей
/// и диапазонов допуска измерений.
/// </summary>
public class MeasurementErrorDefaultsTests
{
  /// <summary>
  /// Проверяет, что для команды EHT используются корректные
  /// пределы измерения, заданные в инструкции.
  /// </summary>
  [Fact]
  public void EhtDisplayInfo_UsesInstructionResistanceRange()
  {
    var displayInfo = MeasurementTypeCommand.EHT.GetCommandDisplayInfo();

    Assert.NotNull(displayInfo);
    Assert.Equal(0.01, displayInfo.LowerLimit);
    Assert.Equal(100, displayInfo.UpperLimit);
  }

  /// <summary>
  /// Проверяет, что для команды EHT используются диапазоны
  /// погрешностей, заданные в инструкции.
  /// </summary>
  [Fact]
  public void EhtDefaultErrors_UseInstructionDefinedRanges()
  {
    var defaults = MeasurementErrorDefaults.GetDefaultsFor(MeasurementTypeCommand.EHT);

    Assert.NotNull(defaults);
    Assert.Collection(defaults.Ranges,
      range =>
      {
        Assert.Equal(0.1, range.MinValue);
        Assert.Equal(1, range.MaxValue);
        Assert.Equal(0.05, range.NumericError);
        Assert.Equal(0, range.PercentageError);
      },
      range =>
      {
        Assert.Equal(1, range.MinValue);
        Assert.Equal(100, range.MaxValue);
        Assert.Equal(0, range.NumericError);
        Assert.Equal(5, range.PercentageError);
      });
  }

  /// <summary>
  /// Проверяет корректность расчёта диапазона допуска
  /// для команды EHT.
  /// </summary>
  /// <param name="measuredValue">Измеренное значение.</param>
  /// <param name="expectedLowerBound">Ожидаемая нижняя граница допуска.</param>
  /// <param name="expectedUpperBound">Ожидаемая верхняя граница допуска.</param>
  /// <param name="expectedDelta">Ожидаемая абсолютная погрешность.</param>
  [Theory]
  [InlineData(0.5, 0.45, 0.55, 0.05)]
  [InlineData(10, 9.5, 10.5, 0.5)]
  public void CalculateToleranceRange_ForEht_UsesInstructionErrors(
    double measuredValue,
    double expectedLowerBound,
    double expectedUpperBound,
    double expectedDelta)
  {
    var (lowerBound, upperBound, delta) =
      MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.EHT, measuredValue);

    Assert.Equal(expectedLowerBound, lowerBound, precision: 10);
    Assert.Equal(expectedUpperBound, upperBound, precision: 10);
    Assert.Equal(expectedDelta, delta, precision: 10);
  }

  /// <summary>
  /// Проверяет, что при измерении значения ниже допустимого диапазона
  /// для команды EHT генерируется исключение.
  /// </summary>
  [Fact]
  public void CalculateToleranceRange_ForEhtBelowDefinedAccuracy_Throws()
  {
    Assert.Throws<InvalidOperationException>(() =>
      MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.EHT, 0.05));
  }
}

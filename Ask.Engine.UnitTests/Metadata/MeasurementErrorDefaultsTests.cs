using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;

namespace Ask.Engine.UnitTests.Metadata;

public class MeasurementErrorDefaultsTests
{
  [Fact]
  public void EhtDisplayInfo_UsesInstructionResistanceRange()
  {
    var displayInfo = MeasurementTypeCommand.EHT.GetDisplayInfo();

    Assert.NotNull(displayInfo);
    Assert.Equal(0.01, displayInfo.LowerLimit);
    Assert.Equal(100, displayInfo.UpperLimit);
  }

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

  [Fact]
  public void CalculateToleranceRange_ForEhtBelowDefinedAccuracy_Throws()
  {
    Assert.Throws<InvalidOperationException>(() =>
      MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.EHT, 0.05));
  }
}

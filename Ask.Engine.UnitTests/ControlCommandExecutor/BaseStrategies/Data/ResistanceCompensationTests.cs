using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.BaseStrategies.Data;

public class ResistanceCompensationTests
{
  [Fact]
  public void SubtractSwitchResistance_WhenDifferenceIsPositive_ReturnsDifference()
  {
    var result = ResistanceCompensation.SubtractSwitchResistance(1.5, 0.2, subtract: true);

    Assert.Equal(1.3, result, precision: 10);
  }

  [Fact]
  public void SubtractSwitchResistance_WhenDifferenceIsNegative_ReturnsZero()
  {
    var result = ResistanceCompensation.SubtractSwitchResistance(0.1, 0.2, subtract: true);

    Assert.Equal(0, result);
  }

  [Fact]
  public void SubtractSwitchResistance_WhenSubtractionIsDisabled_DoesNotSubtractButClamps()
  {
    var positiveResult = ResistanceCompensation.SubtractSwitchResistance(0.1, 0.2, subtract: false);
    var negativeResult = ResistanceCompensation.SubtractSwitchResistance(-0.1, 0.2, subtract: false);

    Assert.Equal(0.1, positiveResult);
    Assert.Equal(0, negativeResult);
  }
}

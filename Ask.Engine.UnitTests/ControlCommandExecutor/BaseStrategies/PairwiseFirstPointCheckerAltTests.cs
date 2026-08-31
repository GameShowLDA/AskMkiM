using Ask.Engine.ControlCommandExecutor.BaseStrategies;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.BaseStrategies;

public class PairwiseFirstPointCheckerAltTests
{
  [Fact]
  public void CanMeasurePair_PreviousPointFailureDoesNotAffectNextPoint()
  {
    bool failedPointCanBeMeasured = PairwiseFirstPointCheckerAlt.CanMeasurePair(false, true);
    bool nextPointCanBeMeasured = PairwiseFirstPointCheckerAlt.CanMeasurePair(false, false);

    Assert.False(failedPointCanBeMeasured);
    Assert.True(nextPointCanBeMeasured);
  }

  [Fact]
  public void CanMeasurePair_BasePointFailurePreventsPairsDependentOnBasePoint()
  {
    Assert.False(PairwiseFirstPointCheckerAlt.CanMeasurePair(true, false));
  }

  [Fact]
  public void CanMeasurePair_CurrentPointFailuresAreIndependentAcrossChainsAndGroups()
  {
    bool[][][] pointFailuresByGroupAndChain =
    [
      [[true, false], [false, true]],
      [[false, false]]
    ];

    var actual = pointFailuresByGroupAndChain
      .Select(group => group
        .Select(chain => chain
          .Select(currentPointError => PairwiseFirstPointCheckerAlt.CanMeasurePair(false, currentPointError))
          .ToArray())
        .ToArray())
      .ToArray();

    Assert.False(actual[0][0][0]);
    Assert.True(actual[0][0][1]);
    Assert.True(actual[0][1][0]);
    Assert.False(actual[0][1][1]);
    Assert.True(actual[1][0][0]);
    Assert.True(actual[1][0][1]);
  }

  [Theory]
  [InlineData(101)]
  [InlineData(200)]
  public void IsPairMeasurementOverload_ResistanceWithinCommandRangeIsNotOverload(double resistance)
  {
    Assert.False(PairwiseFirstPointCheckerAlt.IsPairMeasurementOverload(resistance));
  }

  [Theory]
  [InlineData(double.PositiveInfinity)]
  [InlineData(9.9E+37)]
  public void IsPairMeasurementOverload_MultimeterOverloadValueIsOverload(double resistance)
  {
    Assert.True(PairwiseFirstPointCheckerAlt.IsPairMeasurementOverload(resistance));
  }
}

using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
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

  [Theory]
  [InlineData(99, 100, false)]
  [InlineData(100, 100, false)]
  [InlineData(101, 100, true)]
  [InlineData(double.PositiveInfinity, 200, true)]
  [InlineData(9.9E+37, 200, true)]
  public void IsAboveUpperBound_UsesUpperLimitAndOverloadMarker(
    double resistance,
    double upperBound,
    bool expected)
  {
    Assert.Equal(
      expected,
      EhtHighResistanceLocalizationService.IsAboveUpperBound(resistance, upperBound));
  }

  [Fact]
  public async Task SplitIntoFragmentsAsync_UpperFailuresAreCheckedRecursively()
  {
    var points = new[] { CreatePoint(1), CreatePoint(2), CreatePoint(3), CreatePoint(4) };
    var measurements = new Dictionary<(int First, int Second), double>
    {
      [(1, 2)] = 50,
      [(1, 3)] = 150,
      [(1, 4)] = double.PositiveInfinity,
      [(3, 4)] = 40
    };
    var measuredPairs = new List<(int First, int Second)>();

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      points,
      100,
      (first, second) =>
      {
        var pair = (first.PointNumber, second.PointNumber);
        measuredPairs.Add(pair);
        return Task.FromResult(measurements[pair]);
      });

    Assert.Equal([(1, 2), (1, 3), (1, 4), (3, 4)], measuredPairs);
    Assert.Equal(2, localization.Fragments.Count);
    Assert.Equal([1, 2], localization.Fragments[0].PointModels.Select(point => point.PointNumber));
    Assert.Equal([3, 4], localization.Fragments[1].PointModels.Select(point => point.PointNumber));
    Assert.Equal(150, localization.FirstAboveUpperBound);
  }

  [Fact]
  public async Task SplitIntoFragmentsAsync_OverloadCreatesIndependentFragment()
  {
    var points = new[] { CreatePoint(1), CreatePoint(2) };

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      points,
      200,
      (_, _) => Task.FromResult(9.9E+37));

    Assert.Equal(2, localization.Fragments.Count);
    Assert.Equal(1, Assert.Single(localization.Fragments[0].PointModels).PointNumber);
    Assert.Equal(2, Assert.Single(localization.Fragments[1].PointModels).PointNumber);
    Assert.Equal(9.9E+37, localization.FirstAboveUpperBound);
  }

  [Fact]
  public async Task SplitIntoFragmentsAsync_LowerFailureRemainsInConnectedFragment()
  {
    var points = new[] { CreatePoint(1), CreatePoint(2), CreatePoint(3) };

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      points,
      100,
      (_, _) => Task.FromResult(0d));

    Assert.Single(localization.Fragments);
    Assert.Equal([1, 2, 3], localization.Fragments[0].PointModels.Select(point => point.PointNumber));
    Assert.Null(localization.FirstAboveUpperBound);
  }

  [Fact]
  public async Task SplitIntoFragmentsAsync_MultipleUpperFailuresCreateMultipleFragments()
  {
    var points = new[] { CreatePoint(1), CreatePoint(2), CreatePoint(3), CreatePoint(4) };
    var measurements = new Dictionary<(int First, int Second), double>
    {
      [(1, 2)] = 150,
      [(1, 3)] = 160,
      [(1, 4)] = 50,
      [(2, 3)] = 170
    };

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      points,
      100,
      (first, second) => Task.FromResult(measurements[(first.PointNumber, second.PointNumber)]));

    Assert.Equal(3, localization.Fragments.Count);
    Assert.Equal([1, 4], localization.Fragments[0].PointModels.Select(point => point.PointNumber));
    Assert.Equal(2, Assert.Single(localization.Fragments[1].PointModels).PointNumber);
    Assert.Equal(3, Assert.Single(localization.Fragments[2].PointModels).PointNumber);
  }

  [Fact]
  public async Task SplitIntoFragmentsAsync_EmptyAndSinglePointDoNotMeasure()
  {
    int measurementCount = 0;
    Task<double> Measure(PointModel _, PointModel __)
    {
      measurementCount++;
      return Task.FromResult(0d);
    }

    var empty = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      [],
      100,
      Measure);
    var single = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      [CreatePoint(1)],
      100,
      Measure);

    Assert.Empty(empty.Fragments);
    Assert.Single(single.Fragments);
    Assert.Equal(0, measurementCount);
  }

  private static PointModel CreatePoint(int pointNumber) => new()
  {
    PointNumber = pointNumber,
    Mnemonic = $"P{pointNumber}"
  };
}

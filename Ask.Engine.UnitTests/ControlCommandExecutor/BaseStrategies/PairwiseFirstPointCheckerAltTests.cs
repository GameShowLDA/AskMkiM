using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.BaseStrategies;

public class PairwiseFirstPointCheckerAltTests
{
  [Fact(DisplayName = "ЭТ: ошибка текущей точки не влияет на проверку следующей точки")]
  public void CanMeasurePair_PreviousPointFailureDoesNotAffectNextPoint()
  {
    bool failedPointCanBeMeasured = PairwiseFirstPointCheckerAlt.CanMeasurePair(false, true);
    bool nextPointCanBeMeasured = PairwiseFirstPointCheckerAlt.CanMeasurePair(false, false);

    Assert.False(failedPointCanBeMeasured);
    Assert.True(nextPointCanBeMeasured);
  }

  [Fact(DisplayName = "ЭТ: ошибка базовой точки запрещает измерение зависимых пар")]
  public void CanMeasurePair_BasePointFailurePreventsPairsDependentOnBasePoint()
  {
    Assert.False(PairwiseFirstPointCheckerAlt.CanMeasurePair(true, false));
  }

  [Fact(DisplayName = "ЭТ: ошибки текущих точек из разных цепей и групп обрабатываются независимо")]
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

  [Theory(DisplayName = "ЭТ: сопротивление в допустимом диапазоне не считается перегрузкой")]
  [InlineData(101)]
  [InlineData(200)]
  public void IsPairMeasurementOverload_ResistanceWithinCommandRangeIsNotOverload(double resistance)
  {
    Assert.False(PairwiseFirstPointCheckerAlt.IsPairMeasurementOverload(resistance));
  }

  [Theory(DisplayName = "ЭТ: признак перегрузки мультиметра распознаётся как Overload")]
  [InlineData(double.PositiveInfinity)]
  [InlineData(9.9E+37)]
  public void IsPairMeasurementOverload_MultimeterOverloadValueIsOverload(double resistance)
  {
    Assert.True(PairwiseFirstPointCheckerAlt.IsPairMeasurementOverload(resistance));
  }

  [Theory(DisplayName = "ЭТ: локализация учитывает только превышение верхней границы")]
  [InlineData(99, 100, false)]
  [InlineData(100, 100, false)]
  [InlineData(101, 100, true)]
  [InlineData(double.PositiveInfinity, 200, true)]
  public void IsAboveUpperBound_UsesOnlyUpperLimitForLocalization(
    double resistance,
    double upperBound,
    bool expected)
  {
    Assert.Equal(
      expected,
      EhtHighResistanceLocalizationService.IsAboveUpperBound(resistance, upperBound));
  }

  [Fact(DisplayName = "ЭТ: локализация разбивает цепь только по значениям выше верхней границы")]
  public async Task SplitIntoFragmentsAsync_OnlyValuesAboveUpperBoundSplitChain()
  {
    var first = CreatePoint(1);
    var belowLowerBound = CreatePoint(2);
    var highFirst = CreatePoint(3);
    var highSecond = CreatePoint(4);
    var measurements = new Dictionary<(int First, int Second), double>
    {
      [(1, 2)] = 10,
      [(1, 3)] = 150,
      [(1, 4)] = 160,
      [(3, 4)] = 50
    };

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      [first, belowLowerBound, highFirst, highSecond],
      100,
      (left, right) => Task.FromResult(measurements[(left.PointNumber, right.PointNumber)]));

    Assert.Equal(150, localization.FirstAboveUpperBound);
    Assert.Equal(2, localization.Fragments.Count);
    Assert.Equal([1, 2], localization.Fragments[0].PointModels.Select(point => point.PointNumber));
    Assert.Equal([3, 4], localization.Fragments[1].PointModels.Select(point => point.PointNumber));
  }

  [Fact(DisplayName = "ЭТ: пустой список точек даёт пустой результат локализации")]
  public async Task SplitIntoFragmentsAsync_EmptyPointsReturnsEmptyResult()
  {
    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      [],
      100,
      (_, _) => throw new InvalidOperationException("Измерение не должно выполняться."));

    Assert.Empty(localization.Fragments);
    Assert.Null(localization.FirstAboveUpperBound);
  }

  [Fact(DisplayName = "ЭТ: одиночная точка остаётся единственным фрагментом без измерения")]
  public async Task SplitIntoFragmentsAsync_SinglePointReturnsSingleFragment()
  {
    var point = CreatePoint(1);

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      [point],
      100,
      (_, _) => throw new InvalidOperationException("Измерение не должно выполняться."));

    Assert.Single(localization.Fragments);
    Assert.Same(point, Assert.Single(localization.Fragments[0].PointModels));
    Assert.Null(localization.FirstAboveUpperBound);
  }

  [Fact(DisplayName = "ЭТ: значения на верхней границе сохраняют одну связную цепь")]
  public async Task SplitIntoFragmentsAsync_ValuesAtUpperBoundKeepSingleFragment()
  {
    var points = new[] { CreatePoint(1), CreatePoint(2), CreatePoint(3) };

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      points,
      100,
      (_, _) => Task.FromResult(100d));

    Assert.Single(localization.Fragments);
    Assert.Equal([1, 2, 3], localization.Fragments[0].PointModels.Select(point => point.PointNumber));
    Assert.Null(localization.FirstAboveUpperBound);
  }

  [Fact(DisplayName = "ЭТ: перегрузка мультиметра отделяет точку в новый фрагмент")]
  public async Task SplitIntoFragmentsAsync_OverloadSplitsPointIntoNewFragment()
  {
    var points = new[] { CreatePoint(1), CreatePoint(2) };

    var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
      points,
      200,
      (_, _) => Task.FromResult(double.PositiveInfinity));

    Assert.Equal(2, localization.Fragments.Count);
    Assert.Equal(1, Assert.Single(localization.Fragments[0].PointModels).PointNumber);
    Assert.Equal(2, Assert.Single(localization.Fragments[1].PointModels).PointNumber);
    Assert.Equal(double.PositiveInfinity, localization.FirstAboveUpperBound);
  }

  [Fact(DisplayName = "ЭТ: рекурсивная локализация разделяет несколько независимых разрывов")]
  public async Task SplitIntoFragmentsAsync_RecursiveFailuresCreateIndependentFragments()
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
      (left, right) => Task.FromResult(measurements[(left.PointNumber, right.PointNumber)]));

    Assert.Equal(3, localization.Fragments.Count);
    Assert.Equal([1, 4], localization.Fragments[0].PointModels.Select(point => point.PointNumber));
    Assert.Equal(2, Assert.Single(localization.Fragments[1].PointModels).PointNumber);
    Assert.Equal(3, Assert.Single(localization.Fragments[2].PointModels).PointNumber);
    Assert.Equal(150, localization.FirstAboveUpperBound);
  }

  [Theory(DisplayName = "ЭТ: итоговое сопротивление компенсирует контакты и кабель в рабочем режиме")]
  [InlineData(150, 10, 20, 5, 130)]
  [InlineData(10, 20, 20, 5, 0)]
  public void CalculateFinalResistance_RealModeAppliesCompensation(
    double measured,
    double firstPoint,
    double secondPoint,
    double cable,
    double expected)
  {
    var originalIdleMode = Ask.Core.Services.Config.AppSettings.ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      Ask.Core.Services.Config.AppSettings.ExecutionConfig.SetIdleMode(false);

      var actual = PairwiseFirstPointCheckerAlt.CalculateFinalResistance(
        measured,
        firstPoint,
        secondPoint,
        10,
        200,
        cable);

      Assert.Equal(expected, actual);
    }
    finally
    {
      Ask.Core.Services.Config.AppSettings.ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }

  [Theory(DisplayName = "ЭТ: холостой режим формирует результат согласно настройке симуляции ошибки")]
  [InlineData(false, 105)]
  [InlineData(true, 135)]
  public void CalculateFinalResistance_IdleModeUsesSimulationPolicy(
    bool errorSimulationEnabled,
    double expected)
  {
    var originalIdleMode = Ask.Core.Services.Config.AppSettings.ExecutionConfig.GetIsIdleModeEnabled();
    var originalErrorSimulation = Ask.Core.Services.Config.AppSettings.ExecutionConfig.GetIsErrorSimulationEnabled();
    try
    {
      Ask.Core.Services.Config.AppSettings.ExecutionConfig.SetIdleMode(true);
      Ask.Core.Services.Config.AppSettings.ExecutionConfig.SetIsErrorSimulationMode(errorSimulationEnabled);

      var actual = PairwiseFirstPointCheckerAlt.CalculateFinalResistance(
        150,
        10,
        20,
        10,
        200,
        50);

      Assert.Equal(expected, actual);
    }
    finally
    {
      Ask.Core.Services.Config.AppSettings.ExecutionConfig.SetIdleMode(originalIdleMode);
      Ask.Core.Services.Config.AppSettings.ExecutionConfig.SetIsErrorSimulationMode(originalErrorSimulation);
    }
  }

  private static PointModel CreatePoint(int pointNumber) => new()
  {
    PointNumber = pointNumber,
    Mnemonic = $"P{pointNumber}"
  };
}

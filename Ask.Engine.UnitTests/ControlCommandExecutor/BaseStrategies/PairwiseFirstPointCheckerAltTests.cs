using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
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

  [Theory(DisplayName = "ЭТ: перегрузка предварительного измерения пары направляется в локализацию")]
  [InlineData(true, double.PositiveInfinity, true)]
  [InlineData(true, 9.9E+37, true)]
  [InlineData(true, 200, false)]
  [InlineData(false, double.PositiveInfinity, false)]
  public void ShouldLocalizeInitialPairMeasurement_UsesOverloadWhenValidationEnabled(
    bool validatePointConnections,
    double resistance,
    bool expected)
  {
    Assert.Equal(
      expected,
      PairwiseFirstPointCheckerAlt.ShouldLocalizeInitialPairMeasurement(
        validatePointConnections,
        resistance));
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

  [Fact(DisplayName = "ЭТ: перегрузка формирует одну агрегированную строку итогового протокола")]
  public async Task OverloadLocalization_ProducesSingleAggregatedReportError()
  {
    bool originalMachineAddressVisibility = DeviceDisplayConfig.GetMachineAddressVisibility();
    bool originalLegacyMode = ExecutionConfig.GetIsLegacyCompatibilityModeEnabled();

    try
    {
      DeviceDisplayConfig.SetMachineAddressVisibility(true);
      ExecutionConfig.SetLegacyCompatibilityMode(false);

      var points = Enumerable.Range(71, 5)
        .Concat(Enumerable.Range(81, 5))
        .Select(CreateK1Point)
        .ToArray();
      var measurements = new Dictionary<(int First, int Second), double>
      {
        [(71, 72)] = 0.550,
        [(71, 73)] = 0.567,
        [(71, 74)] = 0.580,
        [(71, 75)] = 0.578,
        [(71, 81)] = double.PositiveInfinity,
        [(71, 82)] = double.PositiveInfinity,
        [(71, 83)] = double.PositiveInfinity,
        [(71, 84)] = double.PositiveInfinity,
        [(71, 85)] = double.PositiveInfinity,
        [(81, 82)] = 0.524,
        [(81, 83)] = 0.558,
        [(81, 84)] = 0.552,
        [(81, 85)] = 0.565,
      };

      var localization = await EhtHighResistanceLocalizationService.SplitIntoFragmentsAsync(
        points,
        40,
        (left, right) => Task.FromResult(measurements[(left.PointNumber, right.PointNumber)]));
      string display = await EhtHighResistanceLocalizationService.GetLocalizationDisplayAsync(
        localization.Fragments);
      var error = EhtHighResistanceLocalizationService.BuildLocalizationErrorMessage(
        MeasurementTypeCommand.EHT,
        display,
        localization.FirstAboveUpperBound!.Value,
        0.01,
        40);

      ProtocolModel.SetErrorsTemplate("$ПРОГРАММА");
      var protocol = new ProtocolModel
      {
        Date = new DateTime(2026, 9, 21),
        Designation = "СТ6737.000.000",
        ControlObjectName = "ТЕСТ",
        Number = "1",
        Executor = string.Empty,
        ProgramName = "ТЕСТ",
        Agent = string.Empty,
        Customer = string.Empty,
        Mode = string.Empty,
      };
      protocol.Errors["160 ЭТ"] = [error];

      string report = ProtocolModel.GetProtocolWithErrorsText(protocol);
      string errorLine = Assert.Single(
        report.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries),
        line => line.StartsWith("ERR", StringComparison.Ordinal));

      Assert.Equal(
        "ERR1 160 ЭТ: *К1/71 [1.4.71],К1/72 [1.4.72],К1/73 [1.4.73],К1/74 [1.4.74],К1/75 [1.4.75]" +
        "**К1/81 [1.4.81],К1/82 [1.4.82],К1/83 [1.4.83],К1/84 [1.4.84],К1/85 [1.4.85]* " +
        "д.б. 0,01<Ом<40: Rизм= Overload [БРАК]",
        errorLine);
    }
    finally
    {
      DeviceDisplayConfig.SetMachineAddressVisibility(originalMachineAddressVisibility);
      ExecutionConfig.SetLegacyCompatibilityMode(originalLegacyMode);
    }
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
  [InlineData(TypeErroneousMeasurement.None)]
  [InlineData(TypeErroneousMeasurement.Low)]
  [InlineData(TypeErroneousMeasurement.High)]
  [InlineData(TypeErroneousMeasurement.Rnd)]
  public void CalculateFinalResistance_IdleModeUsesSimulationPolicy(
    TypeErroneousMeasurement errorSimulation)
  {
    var originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    var originalErrorSimulation = ExecutionConfig.GetErroneousMeasurementType();
    try
    {
      ExecutionConfig.SetIdleMode(true);
      ExecutionConfig.SetErroneousMeasurementType(errorSimulation);

      var actual = PairwiseFirstPointCheckerAlt.CalculateFinalResistance(
        150,
        10,
        20,
        10,
        200,
        50);

      Assert.True(double.IsFinite(actual));
      Assert.True(actual >= 0);
      switch (errorSimulation)
      {
        case TypeErroneousMeasurement.None:
          Assert.Equal(105, actual);
          break;
        case TypeErroneousMeasurement.Low:
          Assert.True(actual < 10);
          break;
        case TypeErroneousMeasurement.High:
          Assert.True(actual > 200);
          break;
        case TypeErroneousMeasurement.Rnd:
          Assert.True(actual < 10 || actual > 200);
          break;
      }
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
      ExecutionConfig.SetErroneousMeasurementType(originalErrorSimulation);
    }
  }

  private static PointModel CreatePoint(int pointNumber) => new()
  {
    PointNumber = pointNumber,
    Mnemonic = $"P{pointNumber}"
  };

  private static PointModel CreateK1Point(int pointNumber) => new()
  {
    DeviceNumber = 1,
    ModuleNumber = 4,
    PointNumber = pointNumber,
    Mnemonic = $"К1/{pointNumber}",
  };
}

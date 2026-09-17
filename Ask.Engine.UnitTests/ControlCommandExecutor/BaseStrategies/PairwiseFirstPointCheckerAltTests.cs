using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using Moq;

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

  [Fact(DisplayName = "ЭТ: локализация публикует скомпенсированный результат пары как промежуточный")]
  public async Task PublishIntermediateMeasurementAsync_PublishesPairResultAsIntermediate()
  {
    bool originalMachineAddressVisibility = DeviceDisplayConfig.GetMachineAddressVisibility();
    try
    {
      DeviceDisplayConfig.SetMachineAddressVisibility(false);
      var resultExecutor = new RecordingMeasurementResultMessageExecutor();
      var messageService = new Mock<IUserInteractionService>();
      var context = new PairwiseFirstPointAltContext
      {
        TypeCommand = MeasurementTypeCommand.EHT,
        LowerLimit = 0.01,
        HigherLimit = 40,
        MessageService = messageService.Object,
        ResultMessageExecutor = resultExecutor,
      };

      await EhtHighResistanceLocalizationService.PublishIntermediateMeasurementAsync(
        context,
        CreatePoint(71),
        CreatePoint(72),
        41.25);

      var publishedContext = resultExecutor.PublishedContext;
      Assert.NotNull(publishedContext);
      Assert.True(publishedContext.IsIntermediate);
      Assert.Equal("P71,P72", publishedContext.MeasurementTarget);
      Assert.Equal(41.25, publishedContext.Range.TargetValue);
      Assert.Equal(0.01, publishedContext.Range.LowerBound);
      Assert.Equal(40, publishedContext.Range.UpperBound);
      Assert.Equal(1, resultExecutor.CallCount);
    }
    finally
    {
      DeviceDisplayConfig.SetMachineAddressVisibility(originalMachineAddressVisibility);
    }
  }

  [Fact(DisplayName = "ЭТ: результат локализации обозначает разрывы звёздочками")]
  public async Task FormatLocalizedBreaksAsync_UsesStarsBetweenDisconnectedFragments()
  {
    bool originalMachineAddressVisibility = DeviceDisplayConfig.GetMachineAddressVisibility();
    try
    {
      DeviceDisplayConfig.SetMachineAddressVisibility(false);
      var fragments = new[]
      {
        new ChainModel([CreatePoint(71), CreatePoint(72)]),
        new ChainModel([CreatePoint(73)]),
      };

      var formatted = await EhtHighResistanceLocalizationService.FormatLocalizedBreaksAsync(fragments);

      Assert.Equal("*P71#P72**P73*", formatted);
      Assert.DoesNotContain(",,", formatted);
    }
    finally
    {
      DeviceDisplayConfig.SetMachineAddressVisibility(originalMachineAddressVisibility);
    }
  }

  [Fact(DisplayName = "ЭТ: локализация ожидает продолжения при паузе")]
  public async Task WaitForExecutionAsync_UsesPauseGate()
  {
    using var cancellation = new CancellationTokenSource();
    var interaction = new Mock<IUserInteractionService>();
    interaction
      .Setup(service => service.GetCancellationToken())
      .Returns(cancellation.Token);
    var pauseGate = interaction.As<IExecutionPauseGate>();
    pauseGate
      .Setup(gate => gate.WaitIfPausedAsync(cancellation.Token))
      .Returns(Task.CompletedTask);

    await EhtHighResistanceLocalizationService.WaitForExecutionAsync(interaction.Object);

    pauseGate.Verify(gate => gate.WaitIfPausedAsync(cancellation.Token), Times.Once);
  }

  [Fact(DisplayName = "ЭТ: локализация прекращается по запросу остановки")]
  public async Task WaitForExecutionAsync_CancellationRequestedThrows()
  {
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    var interaction = new Mock<IUserInteractionService>();
    interaction
      .Setup(service => service.GetCancellationToken())
      .Returns(cancellation.Token);

    await Assert.ThrowsAsync<OperationCanceledException>(
      () => EhtHighResistanceLocalizationService.WaitForExecutionAsync(interaction.Object));
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

  private sealed class RecordingMeasurementResultMessageExecutor : IMeasurementResultMessageExecutor
  {
    internal MeasurementResultMessageContext? PublishedContext { get; private set; }
    internal int CallCount { get; private set; }

    public Task<bool> PublishMeasurementResultAsync(MeasurementResultMessageContext context)
    {
      PublishedContext = context;
      CallCount++;
      return Task.FromResult(false);
    }
  }
}

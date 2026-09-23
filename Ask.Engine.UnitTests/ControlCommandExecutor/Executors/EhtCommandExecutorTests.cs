using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors;
using Ask.Engine.UnitTests.TestInfrastructure;
using Moq;
using System.Reflection;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public sealed class EhtCommandExecutorTests
{
  [Theory(DisplayName = "ЭТ: проверка физического подключения точек отключается только в холостом режиме")]
  [InlineData(false, true)]
  [InlineData(true, false)]
  public void ShouldValidatePointConnections_DependsOnlyOnIdleMode(
    bool idleModeEnabled,
    bool expected)
  {
    bool originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();

    try
    {
      ExecutionConfig.SetIdleMode(idleModeEnabled);

      Assert.Equal(expected, EhtCommandExecutor.ShouldValidatePointConnections());
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }

  [Fact(DisplayName = "ЭТ: реальный маршрут исполнителя локализует Overload в одну ошибку протокола")]
  public async Task ExecuteAsync_OverloadMeasurements_ProducesSingleLocalizedReportError()
  {
    var measurements = CreateExecutionMeasurements();
    using var harness = new EhtExecutionHarness(measurements);

    ProtocolModel protocol = await harness.ExecuteAsync(CreateCommand());

    Assert.Equal(58, harness.MeasurementCallCount);
    Assert.Equal(58, measurements.Count);
    Assert.Single(protocol.Errors);
    Assert.Single(protocol.Errors["160 ЭТ"]);
    Assert.Single(harness.PublishedErrors);

    ProtocolModel.SetErrorsTemplate("$ПРОГРАММА");
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

  public static IEnumerable<object[]> FortyExecutionScenarios()
  {
    double[] connectedValues = [0.01, 0.011, 0.5, 1, 5, 20, 39.999, 40];
    for (int index = 0; index < connectedValues.Length; index++)
    {
      yield return [new EhtExecutionScenario(
        index + 1,
        Enumerable.Repeat(0, 10).ToArray(),
        connectedValues[index],
        UseOverload: false)];
    }

    int[][] fragmentPatterns =
    [
      [0, 0, 0, 0, 0, 1, 1, 1, 1, 1],
      [0, 1, 1, 1, 1, 1, 1, 1, 1, 1],
      [0, 0, 0, 0, 0, 0, 0, 0, 0, 1],
      [0, 1, 0, 1, 0, 1, 0, 1, 0, 1],
      [0, 0, 1, 0, 0, 0, 0, 0, 0, 0],
      [0, 0, 0, 0, 1, 0, 0, 0, 0, 0],
      [0, 0, 0, 0, 0, 0, 0, 1, 0, 0],
      [0, 0, 1, 1, 1, 1, 1, 1, 0, 0],
      [0, 1, 2, 2, 2, 2, 2, 2, 2, 2],
      [0, 0, 0, 1, 1, 1, 2, 2, 2, 2],
      [0, 1, 2, 0, 1, 2, 0, 1, 2, 0],
      [0, 0, 1, 2, 0, 0, 1, 2, 0, 0],
      [0, 1, 1, 2, 2, 0, 0, 1, 1, 2],
      [0, 0, 0, 0, 1, 2, 2, 2, 2, 2],
      [0, 1, 2, 3, 3, 3, 3, 3, 3, 3],
      [0, 0, 1, 1, 2, 2, 3, 3, 3, 3],
      [0, 1, 2, 3, 0, 1, 2, 3, 0, 1],
      [0, 0, 0, 1, 2, 3, 1, 2, 3, 0],
      [0, 1, 1, 2, 2, 3, 3, 0, 0, 0],
      [0, 1, 2, 3, 4, 4, 4, 4, 4, 4],
      [0, 0, 1, 1, 2, 2, 3, 3, 4, 4],
      [0, 1, 2, 3, 4, 0, 1, 2, 3, 4],
      [0, 1, 2, 3, 4, 5, 5, 5, 5, 5],
      [0, 0, 1, 2, 3, 4, 5, 0, 0, 0],
      [0, 1, 2, 3, 4, 5, 6, 6, 6, 6],
      [0, 1, 2, 3, 4, 5, 6, 0, 0, 0],
      [0, 1, 2, 3, 4, 5, 6, 7, 7, 7],
      [0, 1, 2, 3, 4, 5, 6, 7, 0, 0],
      [0, 1, 2, 3, 4, 5, 6, 7, 8, 8],
      [0, 1, 2, 3, 4, 5, 6, 7, 8, 0],
      [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
      [0, 1, 0, 2, 0, 3, 0, 4, 0, 5],
    ];

    for (int index = 0; index < fragmentPatterns.Length; index++)
    {
      int id = connectedValues.Length + index + 1;
      yield return [new EhtExecutionScenario(
        id,
        fragmentPatterns[index],
        connectedValues[index % connectedValues.Length],
        UseOverload: index % 2 == 0)];
    }
  }

  [Theory(DisplayName = "ЭТ: 40 наборов измерений проходят через полный исполнитель")]
  [MemberData(nameof(FortyExecutionScenarios))]
  public async Task ExecuteAsync_FortyMeasurementSets_CoversLocalizationCases(
    EhtExecutionScenario scenario)
  {
    var measurements = CreateScenarioMeasurements(scenario);
    using var harness = new EhtExecutionHarness(measurements);

    ProtocolModel protocol = await harness.ExecuteAsync(CreateCommand());

    Assert.Equal(measurements.Count, harness.MeasurementCallCount);
    bool hasBreak = scenario.Components.Distinct().Count() > 1;
    if (!hasBreak)
    {
      Assert.Empty(protocol.Errors);
      Assert.Empty(harness.PublishedErrors);
      return;
    }

    ShowMessageModel error = Assert.Single(protocol.Errors["160 ЭТ"]);
    Assert.Single(harness.PublishedErrors);

    string expectedDisplay = BuildExpectedDisplay(scenario.Components);
    Assert.Equal($"{expectedDisplay} д.б. 0,01<Ом<40", error.Header);
    if (scenario.UseOverload)
    {
      Assert.Equal("Rизм= Overload", error.Message);
    }
    else
    {
      Assert.DoesNotContain("Overload", error.Message, StringComparison.Ordinal);
    }
  }

  private static List<double> CreateScenarioMeasurements(EhtExecutionScenario scenario)
  {
    var contacts = Enumerable.Range(0, scenario.Components.Length)
      .Select(index => 0.8 + ((scenario.Id + index * 7) % 25) * 0.02)
      .ToArray();
    double highResistance = 40.001 + scenario.Id * 0.125;
    var measurements = new List<double> { contacts[0] };

    for (int index = 1; index < scenario.Components.Length; index++)
    {
      measurements.Add(contacts[index]);
      measurements.Add(GetRawPairMeasurement(scenario, contacts, 0, index, highResistance));
    }

    if (scenario.Components.Skip(1).Any(component => component != scenario.Components[0]))
    {
      AppendLocalizationMeasurements(
        scenario,
        contacts,
        Enumerable.Range(0, scenario.Components.Length).ToList(),
        highResistance,
        measurements);
    }

    return measurements;
  }

  private static void AppendLocalizationMeasurements(
    EhtExecutionScenario scenario,
    IReadOnlyList<double> contacts,
    IReadOnlyList<int> pointIndexes,
    double highResistance,
    List<double> measurements)
  {
    if (pointIndexes.Count < 2)
    {
      return;
    }

    int baseIndex = pointIndexes[0];
    var disconnected = new List<int>();
    foreach (int pointIndex in pointIndexes.Skip(1))
    {
      measurements.Add(contacts[baseIndex]);
      measurements.Add(contacts[pointIndex]);
      measurements.Add(GetRawPairMeasurement(
        scenario,
        contacts,
        baseIndex,
        pointIndex,
        highResistance));

      if (scenario.Components[baseIndex] != scenario.Components[pointIndex])
      {
        disconnected.Add(pointIndex);
      }
    }

    AppendLocalizationMeasurements(
      scenario,
      contacts,
      disconnected,
      highResistance,
      measurements);
  }

  private static double GetRawPairMeasurement(
    EhtExecutionScenario scenario,
    IReadOnlyList<double> contacts,
    int firstIndex,
    int secondIndex,
    double highResistance)
  {
    bool connected = scenario.Components[firstIndex] == scenario.Components[secondIndex];
    if (!connected && scenario.UseOverload)
    {
      return double.PositiveInfinity;
    }

    double compensatedResistance = connected
      ? scenario.ConnectedResistance
      : highResistance;
    return compensatedResistance + (contacts[firstIndex] + contacts[secondIndex]) / 2;
  }

  private static string BuildExpectedDisplay(IReadOnlyList<int> components)
  {
    var componentOrder = components.Distinct().ToList();
    return string.Concat(componentOrder.Select(component =>
    {
      var points = components
        .Select((value, index) => (value, pointNumber: index < 5 ? 71 + index : 76 + index))
        .Where(item => item.value == component)
        .Select(item => $"К1/{item.pointNumber} [1.4.{item.pointNumber}]");
      return $"*{string.Join(',', points)}*";
    }));
  }

  private static EhtCommandModel CreateCommand()
  {
    var points = Enumerable.Range(71, 5)
      .Concat(Enumerable.Range(81, 5))
      .Select(pointNumber => new PointModel
      {
        DeviceNumber = 1,
        ModuleNumber = 4,
        PointNumber = pointNumber,
        Mnemonic = $"К1/{pointNumber}",
        PointType = PointType.Star,
      })
      .ToList();

    return new EhtCommandModel
    {
      CommandNumber = "160",
      StartLineNumber = 160,
      FormattedStartLineNumber = 160,
      SourceLines = ["160 ЭТ тест"],
      LowerLimitResistance = 0.01,
      HigherLimitResistance = 40,
      LowerLimitResistanceSource = "0,01 Ом",
      HigherLimitResistanceSource = "40 Ом",
      ResistanceUnit = "Ом",
      CabelResistance = 0,
      Scheme = new SchemeModel([
        new GroupModel([
          new ChainModel(points)
        ])
      ]),
    };
  }

  private static List<double> CreateExecutionMeasurements()
  {
    const double overload = double.PositiveInfinity;

    var mainRun = new List<double>
    {
      1.185,
      1.155, 1.720,
      1.188, 1.754,
      1.163, 1.754,
      1.191, 1.766,
      1.160, overload,
      1.152, overload,
      1.149, overload,
      1.151, overload,
      1.175, overload,
    };

    var localizationRun = new List<double>
    {
      1.185, 1.155, 1.720,
      1.185, 1.188, 1.754,
      1.185, 1.163, 1.754,
      1.185, 1.191, 1.766,
      1.185, 1.160, overload,
      1.185, 1.152, overload,
      1.185, 1.149, overload,
      1.185, 1.151, overload,
      1.185, 1.175, overload,
      1.160, 1.152, 1.680,
      1.160, 1.149, 1.7125,
      1.160, 1.151, 1.7075,
      1.160, 1.175, 1.7325,
    };

    return [.. mainRun, .. localizationRun];
  }

  private static Mock<IUserInteractionService> CreateUserInteractionService()
  {
    var mock = new Mock<IUserInteractionService>();
    mock.SetupProperty(x => x.Header, string.Empty);
    mock.Setup(x => x.GetCancellationToken()).Returns(CancellationToken.None);
    mock.Setup(x => x.WaitUserActionAsync(
      It.IsAny<bool>(),
      It.IsAny<bool>(),
      It.IsAny<bool>())).ReturnsAsync(UserAction.None);
    mock.Setup(x => x.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()))
      .Returns(Task.CompletedTask);
    mock.Setup(x => x.AppendEmptyLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
    mock.Setup(x => x.MoveToLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
    mock.Setup(x => x.GetLastLineNumber()).Returns(0);
    mock.Setup(x => x.GetText()).Returns(string.Empty);
    mock.SetupGet(x => x.ButtonService).Returns(Mock.Of<IButtonService>());
    return mock;
  }

  public sealed record EhtExecutionScenario(
    int Id,
    int[] Components,
    double ConnectedResistance,
    bool UseOverload)
  {
    public override string ToString() => $"Набор {Id}";
  }

  private sealed class EhtExecutionHarness : IDisposable
  {
    private readonly EquipmentServiceScope _scope;
    private readonly Queue<double> _measurements;
    private readonly int _measurementCount;
    private readonly bool _originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    private readonly bool _originalStepByStepMode = ExecutionConfig.GetIsStepByStepModeEnabled();
    private readonly bool _originalLegacyMode = ExecutionConfig.GetIsLegacyCompatibilityModeEnabled();
    private readonly bool _originalMachineAddressVisibility = DeviceDisplayConfig.GetMachineAddressVisibility();
    private readonly bool _originalExecutionParametersVisibility = DeviceDisplayConfig.GetExecutionParametersVisibility();
    private readonly bool _originalMeasurementResultsVisibility = DeviceDisplayConfig.GetMeasurementResultsVisibility();

    public EhtExecutionHarness(IReadOnlyCollection<double> measurements)
    {
      _measurementCount = measurements.Count;
      _measurements = new Queue<double>(measurements);
      ExecutionConfig.SetIdleMode(false);
      ExecutionConfig.SetStepByStepMode(false);
      ExecutionConfig.SetLegacyCompatibilityMode(false);
      DeviceDisplayConfig.SetMachineAddressVisibility(true);
      DeviceDisplayConfig.SetExecutionParametersVisibility(false);
      DeviceDisplayConfig.SetMeasurementResultsVisibility(false);

      ConsoleMock = CreateUserInteractionService();
      EditorMock = new Mock<ITextEditorAdapter>();

      ContinuityManagerMock = new Mock<IContinuityMeasurement>();
      ContinuityManagerMock
        .Setup(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);
      ContinuityManagerMock
        .Setup(x => x.CheckContinuityAsync(
          It.IsAny<MeasurementRange>(),
          It.IsAny<IUserInteractionService>(),
          It.IsAny<double>()))
        .ReturnsAsync(() => _measurements.Dequeue());

      var meterMock = new Mock<IMultimeter>();
      meterMock.SetupGet(x => x.ContinuityManager).Returns(ContinuityManagerMock.Object);

      var connectorManagerMock = new Mock<IConnectorDeviceBusCommutation>();
      connectorManagerMock
        .Setup(x => x.ConnectMultimeter(SwitchingBusNew.AB1, It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var switchingDeviceMock = new Mock<ISwitchingDevice>();
      switchingDeviceMock.SetupGet(x => x.ConnectorManager).Returns(connectorManagerMock.Object);

      var busManagerMock = new Mock<IBusManager>();
      busManagerMock
        .Setup(x => x.ConnectBusAsync(It.IsAny<SwitchingBus>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var pointManagerMock = new Mock<IPointManager>();
      pointManagerMock
        .Setup(x => x.ConnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);
      pointManagerMock
        .Setup(x => x.DisconnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);
      pointManagerMock
        .Setup(x => x.DisconnectingAllPoint(It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var relayModuleMock = new Mock<IRelaySwitchModule>();
      relayModuleMock.SetupGet(x => x.NumberChassis).Returns(1);
      relayModuleMock.SetupGet(x => x.Number).Returns(4);
      relayModuleMock.SetupGet(x => x.BusType).Returns(SwitchingBusNew.AB1);
      relayModuleMock.SetupGet(x => x.SwitchResistance).Returns(0);
      relayModuleMock.SetupGet(x => x.PointManager).Returns(pointManagerMock.Object);
      relayModuleMock.SetupGet(x => x.BusManager).Returns(busManagerMock.Object);

      var points = CreateCommand().Scheme.GroupModels
        .SelectMany(group => group.ChainModels)
        .SelectMany(chain => chain.PointModels)
        .ToList();
      _scope = new EquipmentServiceScope(
        points,
        [relayModuleMock.Object],
        switchingDeviceMock.Object,
        meterMock.Object);
    }

    public Mock<IUserInteractionService> ConsoleMock { get; }
    public Mock<ITextEditorAdapter> EditorMock { get; }
    public Mock<IContinuityMeasurement> ContinuityManagerMock { get; }
    public List<ErrorItem> PublishedErrors { get; } = [];
    public int MeasurementCallCount => _measurementCount - _measurements.Count;

    public Task<ProtocolModel> ExecuteAsync(EhtCommandModel command)
      => WpfTestHost.RunAsync(async () =>
      {
        var manager = new CommandExecutionManager(
          ConsoleMock.Object,
          EditorMock.Object,
          [command],
          "test.opk");
        manager.AddError += PublishedErrors.Add;

        var context = new CommandExecutionContext(
          manager,
          command,
          ConsoleMock.Object,
          EditorMock.Object,
          "test.opk");
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

        await new EhtCommandExecutor().ExecuteAsync(context, protocol);
        return protocol;
      });

    public void Dispose()
    {
      _scope.Dispose();
      ExecutionConfig.SetIdleMode(_originalIdleMode);
      ExecutionConfig.SetStepByStepMode(_originalStepByStepMode);
      ExecutionConfig.SetLegacyCompatibilityMode(_originalLegacyMode);
      DeviceDisplayConfig.SetMachineAddressVisibility(_originalMachineAddressVisibility);
      DeviceDisplayConfig.SetExecutionParametersVisibility(_originalExecutionParametersVisibility);
      DeviceDisplayConfig.SetMeasurementResultsVisibility(_originalMeasurementResultsVisibility);
    }
  }

  private sealed class EquipmentServiceScope : IDisposable
  {
    public EquipmentServiceScope(
      List<PointModel> analyzedPoints,
      List<IRelaySwitchModule> relayModules,
      ISwitchingDevice switchingDevice,
      IMultimeter fastMeter)
    {
      SetProperty("AnalyzedPoints", analyzedPoints);
      SetProperty("ValidRelayModules", relayModules);
      SetProperty("ValidSwitchingDevice", switchingDevice);
      SetProperty("ValidFastMeter", fastMeter);
      SetProperty("ValidBreakdownTester", null);
    }

    public void Dispose()
    {
      SetProperty("AnalyzedPoints", null);
      SetProperty("ValidRelayModules", null);
      SetProperty("ValidSwitchingDevice", null);
      SetProperty("ValidFastMeter", null);
      SetProperty("ValidBreakdownTester", null);
    }

    private static void SetProperty(string propertyName, object? value)
    {
      var property = typeof(EquipmentService).GetProperty(
        propertyName,
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      var setter = property?.GetSetMethod(nonPublic: true);
      setter?.Invoke(null, [value]);
    }
  }
}

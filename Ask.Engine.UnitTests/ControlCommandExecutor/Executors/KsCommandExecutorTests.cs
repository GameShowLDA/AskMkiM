using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors;
using Moq;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public class KsCommandExecutorTests
{
  /// <summary>
  /// Проверяет, что команда КС с ключом Д при успешном измерении пишет одно информационное сообщение в протокол
  /// и использует обычный режим измерения сопротивления.
  /// </summary>
  [Fact(DisplayName = "КС: ключ Д пишет успешное измерение в документирование")]
  public async Task ExecuteAsync_WithDocumentationKey_WritesInfoToProtocol()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15, "\u0414");

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    harness.ResistanceManagerMock.Verify(x => x.SetResistanceModeAsync(It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ResistanceManagerMock.Verify(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ContinuityManagerMock.Verify(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService>()), Times.Never);
    harness.ConnectorManagerMock.Verify(x => x.ConnectMultimeter(SwitchingBusNew.AB1, It.IsAny<IUserInteractionService>()), Times.Once);
    harness.EditorMock.Verify(x => x.SetActiveLine(3), Times.Once);
  }

  /// <summary>
  /// Проверяет, что ключ Б переключает КС в режим прозвонки:
  /// используется ContinuityManager, а обычное измерение сопротивления не вызывается.
  /// </summary>
  [Fact(DisplayName = "КС: ключ Б переключает исполнение в режим прозвонки")]
  public async Task ExecuteAsync_WithContinuityKey_UsesContinuityMeasurement()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15, "\u0411", "\u0414");

    harness.ContinuityManagerMock
      .Setup(x => x.CheckContinuityAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    harness.ContinuityManagerMock.Verify(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ContinuityManagerMock.Verify(x => x.CheckContinuityAsync(10, 5, 15, It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ResistanceManagerMock.Verify(x => x.SetResistanceModeAsync(It.IsAny<IUserInteractionService>()), Times.Never);
    harness.ResistanceManagerMock.Verify(
      x => x.MeasureResistanceAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<IUserInteractionService>()),
      Times.Never);
  }

  /// <summary>
  /// Проверяет, что без ключа Д успешное измерение не попадает в блок документирования протокола.
  /// </summary>
  [Fact(DisplayName = "КС: без ключа Д успешное измерение не записывается в документирование")]
  public async Task ExecuteAsync_WithoutDocumentationKey_DoesNotWriteInfo()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    Assert.Empty(protocol.Info);
  }

  /// <summary>
  /// Проверяет, что при выходе измеренного сопротивления за допустимый диапазон
  /// команда пишет ошибку в протокол и публикует ошибку выполнения.
  /// </summary>
  [Fact(DisplayName = "КС: выход за диапазон формирует ошибку протокола и execution error")]
  public async Task ExecuteAsync_WhenMeasurementFails_WritesErrorAndPublishesExecutionError()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(20);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    Assert.Empty(protocol.Info);
    Assert.Single(harness.PublishedErrors);
    Assert.Contains("X1", harness.PublishedErrors[0].Description);
    Assert.Contains("X2", harness.PublishedErrors[0].Description);
  }

  /// <summary>
  /// Проверяет, что при ошибочном измерении и наличии ключа Д
  /// команда одновременно пишет ошибку и сохраняет измеренное значение в документирование.
  /// </summary>
  [Fact(DisplayName = "КС: при ошибке с ключом Д измерение попадает и в ошибки, и в документирование")]
  public async Task ExecuteAsync_WhenMeasurementFailsWithDocumentationKey_WritesErrorAndInfo()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15, "\u0414");

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(20);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    Assert.Single(harness.PublishedErrors);
  }

  /// <summary>
  /// Проверяет, что при открытой верхней границе исполнитель использует целевое значение firstValue + 10,
  /// а не пытается измерять по несуществующему верхнему пределу.
  /// </summary>
  [Fact(DisplayName = "КС: без верхней границы используется целевое значение firstValue плюс 10")]
  public async Task ExecuteAsync_WithoutHigherLimit_UsesLowerLimitPlusTenAsMeasurementTarget()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, null, "\u0414");

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(15, 5, -1, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(15);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    harness.ResistanceManagerMock.Verify(x => x.MeasureResistanceAsync(15, 5, -1, It.IsAny<IUserInteractionService>()), Times.Once);
  }

  /// <summary>
  /// Проверяет рабочий режим: из измеренного значения вычитается сопротивление коммутатора,
  /// поэтому результат может перейти из допустимого диапазона в ошибку.
  /// </summary>
  [Fact(DisplayName = "КС: в рабочем режиме вычитается сопротивление коммутатора")]
  public async Task ExecuteAsync_InWorkingMode_SubtractsSwitchResistanceBeforeValidation()
  {
    using var harness = new KsExecutionHarness(switchResistance: 1);
    var command = CreateCommand(0.5, 5);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(1);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    Assert.Empty(protocol.Info);
  }

  /// <summary>
  /// Проверяет защиту от отрицательных значений после вычитания сопротивления коммутатора:
  /// исполнитель должен прижать результат к нулю и показать это в сообщении об ошибке.
  /// </summary>
  [Fact(DisplayName = "КС: отрицательный результат после вычитания сопротивления прижимается к нулю")]
  public async Task ExecuteAsync_WhenSwitchResistanceMakesValueNegative_ClampsMeasurementToZero()
  {
    using var harness = new KsExecutionHarness(switchResistance: 10);
    var command = CreateCommand(0.5, 5);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(5);

    var protocol = await harness.ExecuteAsync(command);
    var error = Assert.Single(protocol.Errors[GetCommandKey(command)]);

    Assert.Contains("0", error.Message);
    Assert.Single(harness.PublishedErrors);
  }

  /// <summary>
  /// Проверяет холостой режим без симуляции ошибок:
  /// сопротивление коммутатора не вычитается, поэтому измерение остается успешным.
  /// </summary>
  [Fact(DisplayName = "КС: в idle-режиме сопротивление коммутатора не вычитается")]
  public async Task ExecuteAsync_InIdleMode_DoesNotSubtractSwitchResistance()
  {
    using var harness = new KsExecutionHarness(switchResistance: 1, idleMode: true);
    var command = CreateCommand(0.5, 5);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(1);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    Assert.Empty(protocol.Info);
  }

  /// <summary>
  /// Проверяет холостой режим с симуляцией ошибок при открытой верхней границе:
  /// исполнитель должен принудительно сформировать ошибку, даже если прибор вернул нормальное значение.
  /// </summary>
  [Fact(DisplayName = "КС: в idle-режиме с симуляцией ошибок формируется принудительный брак")]
  public async Task ExecuteAsync_InIdleModeWithErrorSimulationAndOpenUpperLimit_ForcesFailure()
  {
    using var harness = new KsExecutionHarness(idleMode: true, errorSimulation: true);
    var command = CreateCommand(5, null);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(15, 5, -1, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(15);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    Assert.Empty(protocol.Info);
    Assert.Single(harness.PublishedErrors);
  }

  private static Mock<IUserInteractionService> CreateUserInteractionService()
  {
    var mock = new Mock<IUserInteractionService>();
    mock.SetupProperty(x => x.Header, string.Empty);
    mock.Setup(x => x.GetCancellationToken()).Returns(CancellationToken.None);
    mock.Setup(x => x.WaitUserActionAsync(It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(UserAction.None);
    mock.Setup(x => x.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
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

  private static KsCommandModel CreateCommand(double lowerLimit, double? higherLimit, params string[] keys)
  {
    var pointA = CreatePoint("X1", pointNumber: 1);
    var pointB = CreatePoint("X2", pointNumber: 2, pointType: PointType.Comma);

    return new KsCommandModel
    {
      CommandNumber = "10",
      LowerLimitResistance = lowerLimit,
      HigherLimitResistance = higherLimit,
      LowerLimitResistanceSource = $"{lowerLimit} \u041E\u043C",
      HigherLimitResistanceSource = higherLimit.HasValue ? $"{higherLimit} \u041E\u043C" : "\u221E \u041E\u043C",
      ResistanceUnit = "\u041E\u043C",
      StartLineNumber = 10,
      FormattedStartLineNumber = 3,
      SourceLines = new List<string> { "10 \u041A\u0421 test" },
      AlgorithmKey = keys.ToList(),
      Scheme = new SchemeModel(new List<GroupModel>
      {
        new(new List<ChainModel>
        {
          new(new List<PointModel> { pointA, pointB })
        })
      })
    };
  }

  private static string GetCommandKey(KsCommandModel command)
  {
    return $"{command.CommandNumber} {command.Mnemonic}";
  }

  private static void AssertProtocolMessages(Dictionary<string, List<ShowMessageModel>> bucket, KsCommandModel command, int expectedCount)
  {
    var commandKey = GetCommandKey(command);
    Assert.True(bucket.ContainsKey(commandKey));
    Assert.Equal(expectedCount, bucket[commandKey].Count);
  }

  private static PointModel CreatePoint(string mnemonic, int pointNumber, PointType pointType = PointType.Star)
  {
    return new PointModel
    {
      DeviceNumber = 1,
      ModuleNumber = 1,
      PointNumber = pointNumber,
      Mnemonic = mnemonic,
      PointType = pointType
    };
  }

  private sealed class EquipmentServiceScope : IDisposable
  {
    public EquipmentServiceScope(
      List<PointModel> analyzedPoints,
      List<IRelaySwitchModule> relayModules,
      ISwitchingDevice switchingDevice,
      IFastMeter fastMeter)
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
      setter?.Invoke(null, new[] { value });
    }
  }

  private sealed class KsExecutionHarness : IDisposable
  {
    private readonly EquipmentServiceScope _scope;

    public KsExecutionHarness(double switchResistance = 0, bool idleMode = false, bool errorSimulation = false)
    {
      ExecutionConfig.SetIdleMode(idleMode);
      ExecutionConfig.SetStepByStepMode(false);
      ExecutionConfig.SetIsErrorSimulationMode(errorSimulation);
      DeviceDisplayConfig.SetExecutionParametersVisibility(false);
      DeviceDisplayConfig.SetMeasurementResultsVisibility(false);
      DeviceDisplayConfig.SetMachineAddressVisibility(false);

      ConsoleMock = CreateUserInteractionService();
      EditorMock = new Mock<ITextEditorAdapter>();

      ResistanceManagerMock = new Mock<IResistanceMeasurement>();
      ResistanceManagerMock
        .Setup(x => x.SetResistanceModeAsync(It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      ContinuityManagerMock = new Mock<IContinuityMeasurement>();
      ContinuityManagerMock
        .Setup(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var meterMock = new Mock<IFastMeter>();
      meterMock.SetupGet(x => x.ResistanceManager).Returns(ResistanceManagerMock.Object);
      meterMock.SetupGet(x => x.ContinuityManager).Returns(ContinuityManagerMock.Object);

      ConnectorManagerMock = new Mock<IConnectorDeviceBusCommutation>();
      ConnectorManagerMock
        .Setup(x => x.ConnectMultimeter(SwitchingBusNew.AB1, It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var switchingDeviceMock = new Mock<ISwitchingDevice>();
      switchingDeviceMock.SetupGet(x => x.ConnectorManager).Returns(ConnectorManagerMock.Object);

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

      var relayModuleMock = new Mock<IRelaySwitchModule>();
      relayModuleMock.SetupGet(x => x.NumberChassis).Returns(1);
      relayModuleMock.SetupGet(x => x.Number).Returns(1);
      relayModuleMock.SetupGet(x => x.BusType).Returns(SwitchingBusNew.AB1);
      relayModuleMock.SetupGet(x => x.SwitchResistance).Returns(switchResistance);
      relayModuleMock.SetupGet(x => x.PointManager).Returns(pointManagerMock.Object);
      relayModuleMock.SetupGet(x => x.BusManager).Returns(busManagerMock.Object);

      var pointA = CreatePoint("X1", pointNumber: 1);
      var pointB = CreatePoint("X2", pointNumber: 2, pointType: PointType.Comma);
      _scope = new EquipmentServiceScope(
        analyzedPoints: new List<PointModel> { pointA, pointB },
        relayModules: new List<IRelaySwitchModule> { relayModuleMock.Object },
        switchingDevice: switchingDeviceMock.Object,
        fastMeter: meterMock.Object);
    }

    public Mock<IUserInteractionService> ConsoleMock { get; }
    public Mock<ITextEditorAdapter> EditorMock { get; }
    public Mock<IResistanceMeasurement> ResistanceManagerMock { get; }
    public Mock<IContinuityMeasurement> ContinuityManagerMock { get; }
    public Mock<IConnectorDeviceBusCommutation> ConnectorManagerMock { get; }
    public List<ErrorItem> PublishedErrors { get; } = new();

    public async Task<ProtocolModel> ExecuteAsync(KsCommandModel command)
    {
      return await StaTestHost.RunAsync(async () =>
      {
        var manager = new CommandExecutionManager(
          ConsoleMock.Object,
          EditorMock.Object,
          new List<BaseCommandModel> { command },
          "test.opk");
        manager.AddError += PublishedErrors.Add;

        var context = new CommandExecutionContext(manager, command, ConsoleMock.Object, EditorMock.Object, "test.opk");
        var protocol = new ProtocolModel();

        await new KsCommandExecutor().ExecuteAsync(context, protocol);
        return protocol;
      });
    }

    public void Dispose()
    {
      _scope.Dispose();
      ExecutionConfig.SetIdleMode(false);
      ExecutionConfig.SetStepByStepMode(false);
      ExecutionConfig.SetIsErrorSimulationMode(false);
    }
  }

  private static class StaTestHost
  {
    private static readonly Lazy<Dispatcher> DispatcherInstance = new(CreateDispatcher);

    public static Task<T> RunAsync<T>(Func<Task<T>> action)
    {
      var dispatcher = DispatcherInstance.Value;
      return dispatcher.InvokeAsync(async () => await action()).Task.Unwrap();
    }

    private static Dispatcher CreateDispatcher()
    {
      var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);

      var thread = new Thread(() =>
      {
        EnsureApplicationWithResources();
        ready.SetResult(Dispatcher.CurrentDispatcher);
        Dispatcher.Run();
      });

      thread.IsBackground = true;
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();

      return ready.Task.GetAwaiter().GetResult();
    }

    private static void EnsureApplicationWithResources()
    {
      var application = Application.Current ?? new Application();
      application.Resources["TestsProtocolMessageSuccesForeground"] = new SolidColorBrush(Colors.Green);
      application.Resources["TestsProtocolMessageErrorForeground"] = new SolidColorBrush(Colors.Red);
      application.Resources["TestsProtocolHeaderForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["TestsProtocolMessageForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["TestsProtocolTimeForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["YellowColorSolidColorBrush"] = new SolidColorBrush(Colors.Yellow);
      application.Resources["LightBlueColorSolidColorBrush"] = new SolidColorBrush(Colors.LightBlue);
    }
  }
}

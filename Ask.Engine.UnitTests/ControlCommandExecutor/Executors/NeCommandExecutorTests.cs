using System.Reflection;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
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
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors;
using Ask.Engine.UnitTests.TestInfrastructure;
using Moq;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public sealed class NeCommandExecutorTests : IDisposable
{
  [Fact(DisplayName = "НЭ: успешный ответ завершает измерение")]
  public async Task ExecuteAsync_SuccessfulMeasurement_CompletesNormally()
  {
    using var harness = new NeExecutionHarness();
    int measurementNumber = 0;
    harness.DiodeMock
      .Setup(x => x.CheckDiodeAsync(
        It.IsAny<MeasurementRange>(),
        It.IsAny<IUserInteractionService>(),
        It.IsAny<double>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(() => ++measurementNumber % 2 == 1 ? 0.7 : 9.9E+37);

    ProtocolModel protocol = await harness.ExecuteAsync(CreateNeCommand());

    Assert.Empty(protocol.Errors);
    harness.DiodeMock.Verify(
      x => x.CheckDiodeAsync(
        It.IsAny<MeasurementRange>(),
        It.IsAny<IUserInteractionService>(),
        It.IsAny<double>(),
        It.IsAny<CancellationToken>()),
      Times.AtLeastOnce);
  }

  [Fact(DisplayName = "НЭ: ошибка оборудования завершается отрицательным результатом без исключения")]
  public async Task ExecuteAsync_DeviceFailure_ReturnsControlToCaller()
  {
    using var harness = new NeExecutionHarness();
    harness.DiodeMock
      .Setup(x => x.CheckDiodeAsync(
        It.IsAny<MeasurementRange>(),
        It.IsAny<IUserInteractionService>(),
        It.IsAny<double>(),
        It.IsAny<CancellationToken>()))
      .ThrowsAsync(new DeviceException("Нет ответа мультиметра."));

    ProtocolModel protocol = await harness.ExecuteAsync(CreateNeCommand());

    Assert.NotEmpty(protocol.Errors);
  }

  [Fact(DisplayName = "НЭ: ошибка установки режима показывается в UI, а следующая команда выполняется")]
  public async Task ExecuteAllAsync_SetModeFailure_PublishesUiErrorAndContinuesProgram()
  {
    using var harness = new NeExecutionHarness();
    harness.DiodeMock
      .Setup(x => x.SetDiodeModeAsync(null, It.IsAny<CancellationToken>()))
      .ThrowsAsync(new TimeoutException("Оборудование не ответило за 5 с."));
    var ne = CreateNeCommand();
    var ok = new OkCommandModel
    {
      CommandNumber = "20",
      ObjectName = "Следующая программа",
      ObjectCode = "NEXT",
      SourceLines = ["20 ОК"],
      StartLineNumber = 20,
      FormattedStartLineNumber = 4,
    };

    await harness.ExecuteAllAsync([ne, ok]);

    Assert.Contains(
      harness.Messages,
      message => message.Status == ShowMessageModel.MessageType.Error &&
        message.Message.Contains("продолжит выполнение", StringComparison.Ordinal));
    harness.ConsoleMock.Verify(x => x.CompleteCommandAsync(It.IsAny<bool>()), Times.Exactly(2));
    harness.EditorMock.Verify(x => x.SetActiveLine(4), Times.AtLeastOnce);
  }

  public void Dispose()
  {
    ExecutionConfig.SetIdleMode(false);
    ExecutionConfig.SetStepByStepMode(false);
  }

  private static NeCommandModel CreateNeCommand()
  {
    var points = new List<PointModel>
    {
      CreatePoint("X1", 1, PointType.Star),
      CreatePoint("X2", 2, PointType.Comma),
    };

    return new NeCommandModel
    {
      CommandNumber = "10",
      LowerLimitVoltage = 0.5,
      HigherLimitVoltage = 1,
      LowerLimitVoltageSource = "0,5 В",
      HigherLimitVoltageSource = "1 В",
      VoltageUnit = "В",
      StartLineNumber = 10,
      FormattedStartLineNumber = 3,
      SourceLines = ["10 НЭ test"],
      Scheme = new SchemeModel([new GroupModel([new ChainModel(points)])]),
    };
  }

  private static PointModel CreatePoint(string mnemonic, int number, PointType pointType) => new()
  {
    DeviceNumber = 1,
    ModuleNumber = 1,
    PointNumber = number,
    Mnemonic = mnemonic,
    PointType = pointType,
  };

  private sealed class NeExecutionHarness : IDisposable
  {
    private readonly EquipmentScope _scope;

    public NeExecutionHarness()
    {
      ExecutionConfig.SetIdleMode(false);
      ExecutionConfig.SetStepByStepMode(false);
      DeviceDisplayConfig.SetExecutionParametersVisibility(false);
      DeviceDisplayConfig.SetMeasurementResultsVisibility(false);
      DeviceDisplayConfig.SetMachineAddressVisibility(false);

      ConsoleMock = new Mock<IUserInteractionService>();
      ConsoleMock.SetupProperty(x => x.Header, string.Empty);
      ConsoleMock.Setup(x => x.GetCancellationToken()).Returns(CancellationToken.None);
      ConsoleMock.Setup(x => x.WaitUserActionAsync(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
        .ReturnsAsync(UserAction.None);
      ConsoleMock.Setup(x => x.ShowMessageAsync(
          It.IsAny<ShowMessageModel>(),
          It.IsAny<bool>(),
          It.IsAny<bool>(),
          It.IsAny<bool>(),
          It.IsAny<bool>(),
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<int>()))
        .Callback<ShowMessageModel, bool, bool, bool, bool, string, string, int>(
          (message, _, _, _, _, _, _, _) => Messages.Add(message))
        .Returns(Task.CompletedTask);
      ConsoleMock.Setup(x => x.AppendEmptyLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
      ConsoleMock.Setup(x => x.MoveToLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
      ConsoleMock.Setup(x => x.CompleteCommandAsync(It.IsAny<bool>())).Returns(Task.CompletedTask);
      ConsoleMock.Setup(x => x.GetLastLineNumber()).Returns(0);
      ConsoleMock.Setup(x => x.GetText()).Returns(string.Empty);
      ConsoleMock.SetupGet(x => x.ButtonService).Returns(Mock.Of<IButtonService>());
      EditorMock = new Mock<ITextEditorAdapter>();

      DiodeMock = new Mock<IDiodeMeasurement>();
      DiodeMock.Setup(x => x.SetDiodeModeAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
      var meter = new Mock<IMultimeter>();
      meter.SetupGet(x => x.DiodeManager).Returns(DiodeMock.Object);

      var connector = new Mock<IConnectorDeviceBusCommutation>();
      connector.Setup(x => x.ConnectMultimeter(
        It.IsAny<SwitchingBusNew>(),
        It.IsAny<IUserInteractionService>())).ReturnsAsync(true);
      var switchDevice = new Mock<ISwitchingDevice>();
      switchDevice.SetupGet(x => x.ConnectorManager).Returns(connector.Object);

      var bus = new Mock<IBusManager>();
      bus.Setup(x => x.ConnectBusAsync(It.IsAny<SwitchingBus>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);
      var point = new Mock<IPointManager>();
      point.Setup(x => x.ConnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);
      point.Setup(x => x.DisconnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);
      var relay = new Mock<IRelaySwitchModule>();
      relay.SetupGet(x => x.NumberChassis).Returns(1);
      relay.SetupGet(x => x.Number).Returns(1);
      relay.SetupGet(x => x.BusType).Returns(SwitchingBusNew.AB1);
      relay.SetupGet(x => x.PointManager).Returns(point.Object);
      relay.SetupGet(x => x.BusManager).Returns(bus.Object);

      var analyzedPoints = new List<PointModel>
      {
        CreatePoint("X1", 1, PointType.Star),
        CreatePoint("X2", 2, PointType.Comma),
      };
      _scope = new EquipmentScope(analyzedPoints, relay.Object, switchDevice.Object, meter.Object);
    }

    public Mock<IUserInteractionService> ConsoleMock { get; }
    public Mock<ITextEditorAdapter> EditorMock { get; }
    public Mock<IDiodeMeasurement> DiodeMock { get; }
    public List<ShowMessageModel> Messages { get; } = [];

    public async Task<ProtocolModel> ExecuteAsync(NeCommandModel command)
    {
      return await WpfTestHost.RunAsync(async () =>
      {
        var manager = new CommandExecutionManager(ConsoleMock.Object, EditorMock.Object, [command], "test.opk");
        var context = new CommandExecutionContext(manager, command, ConsoleMock.Object, EditorMock.Object, "test.opk");
        var protocol = new ProtocolModel();
        await new NeCommandExecutor().ExecuteAsync(context, protocol);
        return protocol;
      });
    }

    public Task ExecuteAllAsync(List<BaseCommandModel> commands) =>
      WpfTestHost.RunAsync(() =>
        new CommandExecutionManager(ConsoleMock.Object, EditorMock.Object, commands, "test.opk").ExecuteAllAsync());

    public void Dispose() => _scope.Dispose();
  }

  private sealed class EquipmentScope : IDisposable
  {
    public EquipmentScope(
      List<PointModel> points,
      IRelaySwitchModule relay,
      ISwitchingDevice switchingDevice,
      IMultimeter meter)
    {
      Set("AnalyzedPoints", points);
      Set("ValidRelayModules", new List<IRelaySwitchModule> { relay });
      Set("ValidSwitchingDevice", switchingDevice);
      Set("ValidFastMeter", meter);
      Set("ValidBreakdownTester", null);
    }

    public void Dispose()
    {
      Set("AnalyzedPoints", null);
      Set("ValidRelayModules", null);
      Set("ValidSwitchingDevice", null);
      Set("ValidFastMeter", null);
      Set("ValidBreakdownTester", null);
    }

    private static void Set(string propertyName, object? value)
    {
      PropertyInfo? property = typeof(EquipmentService).GetProperty(
        propertyName,
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      property?.GetSetMethod(nonPublic: true)?.Invoke(null, [value]);
    }
  }
}

using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors;
using Moq;
using System.Globalization;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public sealed class ViCommandExecutorTests : IDisposable
{
  [Fact]
  public async Task ExecuteAsync_ConfigureAndConnect_SwitchesOnlyWithoutOutputVoltage()
  {
    var operations = new List<string>();
    var equipment = CreateEquipment(operations);
    var command = CreateCommand(ViOperation.ConfigureAndConnect);
    command.Voltage = 24m;
    command.Current = 0.4m;
    command.PositiveBus = SwitchingBus.A2;
    command.NegativeBus = SwitchingBus.B2;

    await ExecuteAsync(command, equipment.PowerSupply.Object, equipment.SwitchingDevice.Object);

    Assert.Equal(
      ["source:init", "switch:init", "output:off", "current:0.4", "voltage:24", "pint:connect:AB2:delay", "output:on", "mode:ConstantVoltage"],
      operations);
  }

  [Fact]
  public async Task ExecuteAsync_ConfigureWithoutBuses_DisconnectsPintAndEnablesOutput()
  {
    var operations = new List<string>();
    var equipment = CreateEquipment(
      operations,
      [new DeviceConnectionInfo(SwitchingBusNew.AB3, "ПИНТ")]);
    var command = CreateCommand(ViOperation.ConfigureWithoutBuses);
    command.Voltage = 12m;
    command.Current = 0.1m;

    await ExecuteAsync(command, equipment.PowerSupply.Object, equipment.SwitchingDevice.Object);

    Assert.Equal(
      ["source:init", "switch:init", "output:off", "current:0.1", "voltage:12", "pint:disconnect:AB3:delay", "output:on"],
      operations);
  }

  [Fact]
  public async Task ExecuteAsync_DisconnectAndRelease_TurnsOutputOffAndResetsSetpoints()
  {
    var operations = new List<string>();
    var equipment = CreateEquipment(
      operations,
      [new DeviceConnectionInfo(SwitchingBusNew.AB2, "ПИНТ")],
      minimumVoltage: 0.1m,
      minimumCurrent: 0.01m);
    var command = CreateCommand(ViOperation.DisconnectAndRelease);

    await ExecuteAsync(command, equipment.PowerSupply.Object, equipment.SwitchingDevice.Object);

    Assert.Equal(
      ["source:init", "switch:init", "output:off", "pint:disconnect:AB2:delay", "voltage:0.1", "current:0.01"],
      operations);
  }

  [Fact]
  public async Task ExecuteAsync_ConnectionFailure_DoesNotEnableOutput()
  {
    var operations = new List<string>();
    var equipment = CreateEquipment(operations, pintConnectionResult: false);
    var command = CreateCommand(ViOperation.ConfigureAndConnect);
    command.Voltage = 24m;
    command.Current = 0.4m;
    command.PositiveBus = SwitchingBus.A2;
    command.NegativeBus = SwitchingBus.B2;

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      ExecuteAsync(command, equipment.PowerSupply.Object, equipment.SwitchingDevice.Object));

    Assert.DoesNotContain("output:on", operations);
  }

  [Fact]
  public async Task ExecuteAsync_CurrentRegulation_DisablesOutputAndDisconnectsPint()
  {
    var operations = new List<string>();
    var equipment = CreateEquipment(
      operations,
      [new DeviceConnectionInfo(SwitchingBusNew.AB2, "ПИНТ")],
      regulationMode: PowerSupplyRegulationMode.ConstantCurrent);
    var command = CreateCommand(ViOperation.ConfigureAndConnect);
    command.Voltage = 24m;
    command.Current = 0.4m;
    command.PositiveBus = SwitchingBus.A2;
    command.NegativeBus = SwitchingBus.B2;

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      ExecuteAsync(command, equipment.PowerSupply.Object, equipment.SwitchingDevice.Object));

    Assert.Equal("output:off", operations[^2]);
    Assert.Equal("pint:disconnect:AB2:delay", operations[^1]);
  }

  [Fact]
  public async Task ExecuteAsync_WithFastCondition_SkipsPintSwitchingDelay()
  {
    var operations = new List<string>();
    var equipment = CreateEquipment(operations);
    var command = CreateCommand(ViOperation.ConfigureAndConnect);
    command.AlgorithmKey.Add("Б");
    command.Voltage = 24m;
    command.Current = 0.4m;
    command.PositiveBus = SwitchingBus.A2;
    command.NegativeBus = SwitchingBus.B2;

    await ExecuteAsync(command, equipment.PowerSupply.Object, equipment.SwitchingDevice.Object);

    Assert.Contains("pint:connect:AB2:no-delay", operations);
  }

  [Fact]
  public async Task ExecuteAsync_BusPairWithoutRelayModule_RejectsConnection()
  {
    var operations = new List<string>();
    var equipment = CreateEquipment(operations);
    var command = CreateCommand(ViOperation.ConfigureAndConnect);
    command.Voltage = 24m;
    command.Current = 0.4m;
    command.PositiveBus = SwitchingBus.A2;
    command.NegativeBus = SwitchingBus.B2;

    await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(
      command,
      equipment.PowerSupply.Object,
      equipment.SwitchingDevice.Object,
      SupportedBusPairs.AB3));

    Assert.Empty(operations);
  }

  public void Dispose() => EquipmentService.ClearUsedDevices();

  private static async Task ExecuteAsync(
    ViCommandModel command,
    IPowerSupply powerSupply,
    ISwitchingDevice switchingDevice,
    SupportedBusPairs modulePairs = SupportedBusPairs.All)
  {
    var interaction = new Mock<IUserInteractionService>();
    interaction.Setup(service => service.GetCancellationToken()).Returns(CancellationToken.None);
    interaction.Setup(service => service.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()))
      .Returns(Task.CompletedTask);
    var editor = new Mock<ITextEditorAdapter>();
    var manager = new CommandExecutionManager(interaction.Object, editor.Object, [command], null);
    var context = new CommandExecutionContext(manager, command, interaction.Object, editor.Object, string.Empty);
    var executor = new ViCommandExecutor(
      (_, _, _) => Task.FromResult<IPowerSupply?>(powerSupply),
      (_, _) => Task.FromResult<ISwitchingDevice?>(switchingDevice),
      (_, _) => Task.FromResult(modulePairs));

    await executor.ExecuteAsync(context, new ProtocolModel());
  }

  private static ViCommandModel CreateCommand(ViOperation operation) => new()
  {
    CommandNumber = "10",
    SourceLines = ["10 ВИ *1*"],
    PowerSupplyNumber = 1,
    PowerSupplyDeviceNumber = 17,
    PowerSupplyChassisNumber = 2,
    Operation = operation
  };

  private static EquipmentMocks CreateEquipment(
    List<string> operations,
    IReadOnlyList<DeviceConnectionInfo>? connections = null,
    decimal minimumVoltage = 0m,
    decimal minimumCurrent = 0m,
    bool pintConnectionResult = true,
    PowerSupplyRegulationMode regulationMode = PowerSupplyRegulationMode.ConstantVoltage)
  {
    var sourceConnectable = new Mock<IConnectable>();
    sourceConnectable
      .Setup(manager => manager.InitializeAsync(It.IsAny<IUserInteractionService>()))
      .Callback(() => operations.Add("source:init"))
      .ReturnsAsync((true, string.Empty));
    var switchConnectable = new Mock<IConnectable>();
    switchConnectable
      .Setup(manager => manager.InitializeAsync(It.IsAny<IUserInteractionService>()))
      .Callback(() => operations.Add("switch:init"))
      .ReturnsAsync((true, string.Empty));

    var output = new Mock<IOutputManager>();
    output.Setup(manager => manager.SetOutputStateAsync(
        It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<IUserInteractionService>()))
      .Callback<bool, CancellationToken, IUserInteractionService?>((enabled, _, _) =>
        operations.Add(enabled ? "output:on" : "output:off"))
      .Returns(Task.CompletedTask);
    output.Setup(manager => manager.GetRegulationModeAsync(
        It.IsAny<CancellationToken>(), It.IsAny<IUserInteractionService>()))
      .Callback(() => operations.Add($"mode:{regulationMode}"))
      .ReturnsAsync(regulationMode);
    var voltage = new Mock<IVoltageManager>();
    voltage.SetupProperty(manager => manager.MinVoltage, minimumVoltage);
    voltage.Setup(manager => manager.SetVoltageAsync(
        It.IsAny<decimal>(), It.IsAny<CancellationToken>(), It.IsAny<IUserInteractionService>()))
      .Callback<decimal, CancellationToken, IUserInteractionService?>((value, _, _) =>
        operations.Add($"voltage:{value.ToString(CultureInfo.InvariantCulture)}"))
      .Returns(Task.CompletedTask);
    var current = new Mock<ICurrentManager>();
    current.SetupProperty(manager => manager.MinCurrent, minimumCurrent);
    current.Setup(manager => manager.SetCurrentAsync(
        It.IsAny<decimal>(), It.IsAny<CancellationToken>(), It.IsAny<IUserInteractionService>()))
      .Callback<decimal, CancellationToken, IUserInteractionService?>((value, _, _) =>
        operations.Add($"current:{value.ToString(CultureInfo.InvariantCulture)}"))
      .Returns(Task.CompletedTask);

    var connector = new Mock<IConnectorDeviceBusCommutation>();
    connector.Setup(manager => manager.GetConnectedDevices())
      .Returns(connections ?? Array.Empty<DeviceConnectionInfo>());
    connector.Setup(manager => manager.ConnectPINT(
        It.IsAny<SwitchingBusNew>(), It.IsAny<IUserInteractionService>(), It.IsAny<bool>()))
      .Callback<SwitchingBusNew, IUserInteractionService?, bool>((bus, _, skipDelay) =>
        operations.Add($"pint:connect:{bus}:{(skipDelay ? "no-delay" : "delay")}"))
      .ReturnsAsync(pintConnectionResult);
    connector.Setup(manager => manager.DisconnectPINT(
        It.IsAny<SwitchingBusNew>(), It.IsAny<IUserInteractionService>(), It.IsAny<bool>()))
      .Callback<SwitchingBusNew, IUserInteractionService?, bool>((bus, _, skipDelay) =>
        operations.Add($"pint:disconnect:{bus}:{(skipDelay ? "no-delay" : "delay")}"))
      .ReturnsAsync(true);

    var powerSupply = new Mock<IPowerSupply>();
    powerSupply.SetupProperty(device => device.Number, 1);
    powerSupply.SetupProperty(device => device.NumberChassis, 2);
    powerSupply.SetupProperty(device => device.ConnectionDetails, "source");
    powerSupply.SetupGet(device => device.SupportedBusPairs)
      .Returns(SupportedBusPairs.AB2 | SupportedBusPairs.AB3 | SupportedBusPairs.AB4);
    powerSupply.SetupGet(device => device.ConnectableManager).Returns(sourceConnectable.Object);
    powerSupply.SetupGet(device => device.OutputManager).Returns(output.Object);
    powerSupply.SetupGet(device => device.VoltageManager).Returns(voltage.Object);
    powerSupply.SetupGet(device => device.CurrentManager).Returns(current.Object);

    var switchingDevice = new Mock<ISwitchingDevice>();
    switchingDevice.SetupProperty(device => device.Number, 1);
    switchingDevice.SetupProperty(device => device.NumberChassis, 2);
    switchingDevice.SetupProperty(device => device.ConnectionDetails, "switch");
    switchingDevice.SetupProperty(device => device.SupportedBusPairs, SupportedBusPairs.All);
    switchingDevice.SetupGet(device => device.ConnectableManager).Returns(switchConnectable.Object);
    switchingDevice.SetupGet(device => device.ConnectorManager).Returns(connector.Object);

    return new EquipmentMocks(powerSupply, switchingDevice);
  }

  private sealed record EquipmentMocks(
    Mock<IPowerSupply> PowerSupply,
    Mock<ISwitchingDevice> SwitchingDevice);
}

using Ask.Device.Emulator.ModuleRelayControl;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;
using ModuleRelayControlDevice = Ask.Device.Runtime.Device.ModuleRelayControl;

namespace Ask.Device.Emulator.UnitTests.ModuleRelayControl;

public sealed class ModuleRelayControlResponseProcessorTests
{
  [Theory(DisplayName = "МКР: ответ подключения или отключения точки соответствует отправленной команде")]
  [InlineData(true, false, "8.10.2.1")]
  [InlineData(false, false, "8.10.2.2")]
  [InlineData(true, true, "82.10.2.1")]
  [InlineData(false, true, "82.10.2.2")]
  public async Task PointOperation_ValidResponse_ReturnsTrue(
    bool connect,
    bool useHardwareVerification,
    string command)
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(() => 4, () => 2, () => false);
    string response = await protocol.QueryAsync(command);
    ModuleRelayControlDevice module = CreateModule();

    bool result = await ((connect, useHardwareVerification) switch
    {
      (true, false) => ModuleRelayControlResponseProcessor.CheckPointConnectionAsync(response, module, 10, 2),
      (false, false) => ModuleRelayControlResponseProcessor.CheckPointDisconnectionAsync(response, module, 10, 2),
      (true, true) => ModuleRelayControlResponseProcessor.CheckVerifiedPointConnectionAsync(response, module, 10, 2),
      (false, true) => ModuleRelayControlResponseProcessor.CheckVerifiedPointDisconnectionAsync(response, module, 10, 2)
    });

    Assert.True(result);
  }

  [Fact(DisplayName = "МКР: ответ другого модуля отклоняется")]
  public async Task PointOperation_DifferentModule_ReturnsFalse()
  {
    const string response =
      "{\"ModuleName\":\"MKR\",\"NumberDevice\":5,\"NumberChassis\":2," +
      "\"Answer\":\"8.10.2.1\",\"NotDefaultState\":true}";

    bool result = await ModuleRelayControlResponseProcessor.CheckPointConnectionAsync(
      response, CreateModule(), 10, 2);

    Assert.False(result);
  }

  [Fact(DisplayName = "МКР: ответ с параметрами другой точки отклоняется")]
  public async Task PointOperation_DifferentCommandParameters_ReturnsFalse()
  {
    const string response =
      "{\"ModuleName\":\"MKR\",\"NumberDevice\":4,\"NumberChassis\":2," +
      "\"Answer\":\"8.11.2.1\",\"NotDefaultState\":true}";

    bool result = await ModuleRelayControlResponseProcessor.CheckPointConnectionAsync(
      response, CreateModule(), 10, 2);

    Assert.False(result);
  }

  [Fact(DisplayName = "МКР: неподтверждённое состояние реле отклоняется")]
  public async Task VerifiedPointOperation_UncheckedResponse_ReturnsFalse()
  {
    const string response =
      "{\"ModuleName\":\"MKR\",\"NumberDevice\":4,\"NumberChassis\":2," +
      "\"Answer\":\"82.10.2.1\",\"NotDefaultState\":true,\"Checked\":false}";

    bool result = await ModuleRelayControlResponseProcessor.CheckVerifiedPointConnectionAsync(
      response, CreateModule(), 10, 2);

    Assert.False(result);
  }

  [Theory(DisplayName = "МКР: пустой или повреждённый JSON отклоняется")]
  [InlineData("")]
  [InlineData("not-json")]
  [InlineData("{}")]
  public async Task PointOperation_InvalidJson_ReturnsFalse(string response)
  {
    bool result = await ModuleRelayControlResponseProcessor.CheckPointConnectionAsync(
      response, CreateModule(), 10, 2);

    Assert.False(result);
  }

  [Theory]
  [InlineData("4.3.1.1", "bus-connect")]
  [InlineData("4.3.1.2", "bus-disconnect")]
  [InlineData("5.1", "meter-connect")]
  [InlineData("5.2", "meter-disconnect")]
  [InlineData("11.1.10.11", "range-connect")]
  [InlineData("11.1.10.12", "range-disconnect")]
  [InlineData("81.7.1.0", "point-reconnect")]
  public async Task OrdinaryCommand_ValidResponse_ReturnsTrue(string command, string operation)
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(() => 4, () => 2, () => false);
    string response = await protocol.QueryAsync(command);
    ModuleRelayControlDevice module = CreateModule();

    bool result = operation switch
    {
      "bus-connect" => await ModuleRelayControlResponseProcessor.CheckBusOperationAsync(
        response, module, Ask.Core.Shared.Metadata.Enums.DeviceEnums.SwitchingBus.AB1, 3, 1, true),
      "bus-disconnect" => await ModuleRelayControlResponseProcessor.CheckBusOperationAsync(
        response, module, Ask.Core.Shared.Metadata.Enums.DeviceEnums.SwitchingBus.AB1, 3, 1, false),
      "meter-connect" => await ModuleRelayControlResponseProcessor.CheckMeterOperationAsync(
        response, module, true),
      "meter-disconnect" => await ModuleRelayControlResponseProcessor.CheckMeterOperationAsync(
        response, module, false),
      "range-connect" => await ModuleRelayControlResponseProcessor.CheckPointRangeOperationAsync(
        response, module, 1, 10, Ask.Core.Shared.Metadata.Enums.DeviceEnums.BusPoint.A, true),
      "range-disconnect" => await ModuleRelayControlResponseProcessor.CheckPointRangeOperationAsync(
        response, module, 1, 10, Ask.Core.Shared.Metadata.Enums.DeviceEnums.BusPoint.A, false),
      "point-reconnect" => await ModuleRelayControlResponseProcessor.CheckPointReconnectionAsync(
        response, module, 7, Ask.Core.Shared.Metadata.Enums.DeviceEnums.BusPoint.A),
      _ => false,
    };

    Assert.True(result);
  }

  [Fact]
  public async Task InitializationAndReset_ValidResponses_ReturnTrue()
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(() => 4, () => 2, () => false);
    ModuleRelayControlDevice module = CreateModule();

    string initialization = await protocol.QueryAsync("1.0.0.0");
    string reset = await protocol.QueryAsync("2.1.0.0");

    Assert.True(ModuleRelayControlResponseProcessor.CheckInitialization(initialization, module));
    Assert.True(ModuleRelayControlResponseProcessor.CheckReset(reset, module));
  }

  [Fact]
  public void FirmwareError_IsConvertedToProtocolException()
  {
    const string response =
      "{\"ModuleName\":\"MKR\",\"NumberDevice\":4,\"NumberChassis\":2," +
      "\"Status\":\"InvalidParametr\"}";

    Assert.Throws<Ask.Core.Services.Errors.Device.ModuleRelayControl.ModuleRelayControlProtocolException>(
      () => ModuleRelayControlResponseProcessor.EnsureCommandAccepted(response, CreateModule(), "4.3.1.1"));
  }

  private static ModuleRelayControlDevice CreateModule()
  {
    return new ModuleRelayControlDevice
    {
      NumberChassis = 2,
      Number = 4
    };
  }
}

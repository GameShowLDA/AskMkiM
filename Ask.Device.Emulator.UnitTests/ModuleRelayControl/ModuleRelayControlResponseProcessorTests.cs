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

  private static ModuleRelayControlDevice CreateModule()
  {
    return new ModuleRelayControlDevice
    {
      NumberChassis = 2,
      Number = 4
    };
  }
}

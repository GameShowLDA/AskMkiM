using Ask.Device.Emulator.ModuleRelayControl;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;

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

    bool result = connect
      ? ModuleRelayControlResponseProcessor.CheckPointConnection(
        response, 2, 4, 10, 2, useHardwareVerification)
      : ModuleRelayControlResponseProcessor.CheckPointDisconnection(
        response, 2, 4, 10, 2, useHardwareVerification);

    Assert.True(result);
  }

  [Fact(DisplayName = "МКР: ответ другого модуля отклоняется")]
  public void PointOperation_DifferentModule_ReturnsFalse()
  {
    const string response =
      "{\"ModuleName\":\"MKR\",\"NumberDevice\":5,\"NumberChassis\":2," +
      "\"Answer\":\"8.10.2.1\",\"NotDefaultState\":true}";

    bool result = ModuleRelayControlResponseProcessor.CheckPointConnection(
      response, 2, 4, 10, 2);

    Assert.False(result);
  }

  [Fact(DisplayName = "МКР: ответ с параметрами другой точки отклоняется")]
  public void PointOperation_DifferentCommandParameters_ReturnsFalse()
  {
    const string response =
      "{\"ModuleName\":\"MKR\",\"NumberDevice\":4,\"NumberChassis\":2," +
      "\"Answer\":\"8.11.2.1\",\"NotDefaultState\":true}";

    bool result = ModuleRelayControlResponseProcessor.CheckPointConnection(
      response, 2, 4, 10, 2);

    Assert.False(result);
  }

  [Fact(DisplayName = "МКР: неподтверждённое состояние реле отклоняется")]
  public void VerifiedPointOperation_UncheckedResponse_ReturnsFalse()
  {
    const string response =
      "{\"ModuleName\":\"MKR\",\"NumberDevice\":4,\"NumberChassis\":2," +
      "\"Answer\":\"82.10.2.1\",\"NotDefaultState\":true,\"Checked\":false}";

    bool result = ModuleRelayControlResponseProcessor.CheckPointConnection(
      response, 2, 4, 10, 2, useHardwareVerification: true);

    Assert.False(result);
  }

  [Theory(DisplayName = "МКР: пустой или повреждённый JSON отклоняется")]
  [InlineData("")]
  [InlineData("not-json")]
  [InlineData("{}")]
  public void PointOperation_InvalidJson_ReturnsFalse(string response)
  {
    bool result = ModuleRelayControlResponseProcessor.CheckPointConnection(
      response, 2, 4, 10, 2);

    Assert.False(result);
  }
}

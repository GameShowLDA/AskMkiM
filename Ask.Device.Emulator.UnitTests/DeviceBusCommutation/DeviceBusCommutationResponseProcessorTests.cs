using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;
using SwitchingDevice = Ask.Device.Runtime.Device.DeviceBusCommutation;

namespace Ask.Device.Emulator.UnitTests.DeviceBusCommutation;

public sealed class DeviceBusCommutationResponseProcessorTests
{
  [Theory(DisplayName = "УКШ: JSON-ответ игнорирует только завершающую точку Answer")]
  [InlineData("5.7.0.1", "5.7.0.1")]
  [InlineData("5.7.0.1.", "5.7.0.1")]
  [InlineData("5.7.0.1", "5.7.0.1.")]
  [InlineData("5.7.0.1.", "5.7.0.1.")]
  public void JsonCommand_OptionalTrailingPoint_ReturnsTrue(string answer, string expectedAnswer)
  {
    SwitchingDevice device = CreateDevice();
    string response = $$"""
      {"ModuleName":"DeviceBusCommutation","NumberDevice":20,"NumberChassis":1,"Answer":"{{answer}}"}
      """;

    Assert.True(DeviceBusCommutationResponseProcessor.CheckJsonCommand(response, device, expectedAnswer));
  }

  [Theory(DisplayName = "УКШ: JSON-ответ с другим подтверждением отклоняется")]
  [InlineData("5.7.0.2.")]
  [InlineData("prefix.5.7.0.1.")]
  [InlineData("5.7.0.1.extra")]
  public void JsonCommand_DifferentAnswer_ReturnsFalse(string answer)
  {
    SwitchingDevice device = CreateDevice();
    string response = $$"""
      {"ModuleName":"DeviceBusCommutation","NumberDevice":20,"NumberChassis":1,"Answer":"{{answer}}"}
      """;

    Assert.False(DeviceBusCommutationResponseProcessor.CheckJsonCommand(response, device, "5.7.0.1."));
  }

  [Fact(DisplayName = "УКШ: ответ от другого устройства отклоняется")]
  public void JsonCommand_WrongDevice_ReturnsFalse()
  {
    SwitchingDevice device = CreateDevice();
    const string response = """
      {"ModuleName":"DeviceBusCommutation","NumberDevice":21,"NumberChassis":1,"Answer":"7.1."}
      """;

    Assert.False(DeviceBusCommutationResponseProcessor.CheckJsonCommand(response, device, "7.1."));
  }

  [Theory(DisplayName = "УКШ: числовой ответ проверяется строго")]
  [InlineData("0", 0, true)]
  [InlineData("17", 17, true)]
  [InlineData("17", 18, false)]
  [InlineData("error", 0, false)]
  public void NumericCommand_ReturnsExpectedResult(string response, int expected, bool result)
    => Assert.Equal(result, DeviceBusCommutationResponseProcessor.CheckNumericCommand(response, expected));

  [Fact(DisplayName = "УКШ: инициализация и сброс проверяют формат прошивки")]
  public void InitializationAndReset_ValidResponses_ReturnTrue()
  {
    SwitchingDevice device = CreateDevice();

    Assert.True(DeviceBusCommutationResponseProcessor.CheckInitialization(
      "{\"ModuleName\":\"DeviceBusCommutation\",\"NumberDevice\":20,\"NumberChassis\":1}", device));
    Assert.True(DeviceBusCommutationResponseProcessor.CheckReset(
      "{\"ModuleName\":\"DeviceBusCommutation\",\"NumberDevice\":20,\"NumberChassis\":1,\"Answer\":\"2.0.1.\"}", device));
  }

  [Theory(DisplayName = "УКШ: обработчик шин знает сокращённый формат ответа прошивки")]
  [InlineData(true, "7.1.", true)]
  [InlineData(false, "7.2.", true)]
  [InlineData(true, "7.2.", false)]
  public async Task AllBuses_UsesFirmwareAnswer(bool connect, string answer, bool expected)
  {
    SwitchingDevice device = CreateDevice();
    string response = $$"""
      {"ModuleName":"DeviceBusCommutation","NumberDevice":20,"NumberChassis":1,"Answer":"{{answer}}"}
      """;

    bool result = await DeviceBusCommutationResponseProcessor.CheckAllBusesOperationAsync(
      response, device, connect);

    Assert.Equal(expected, result);
  }

  [Theory(DisplayName = "УКШ: ответы самоконтроля проверяются по формату соответствующей команды")]
  [InlineData("1", true)]
  [InlineData("0", false)]
  public void SelfTestRelayControl_UsesNumericFirmwareAnswer(string response, bool expected)
    => Assert.Equal(expected, DeviceBusCommutationResponseProcessor.CheckSelfTestRelayControl(response));

  private static SwitchingDevice CreateDevice()
    => new() { Number = 20, NumberChassis = 1 };
}

using Ask.Device.Emulator.DeviceBusCommutation;
using System.Text.Json;

namespace Ask.Device.Emulator.UnitTests.DeviceBusCommutation;

public sealed class DeviceBusCommutationEmulatorProtocolTests
{
  [Theory(DisplayName = "УКШ: JSON-команда возвращает идентификаторы устройства и подтверждение прошивки")]
  [InlineData("4.1.2.1", "4.1.2.1.")]
  [InlineData("5.7.0.2", "5.7.0.2.")]
  [InlineData("7.1.0.0", "7.1.0.0.")]
  [InlineData("9.2.0.1", "9.2.0.1.")]
  public async Task JsonCommand_ReturnsEnvelope(string command, string expectedAnswer)
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = CreateProtocol();

    using JsonDocument response = JsonDocument.Parse(await protocol.QueryAsync(command));

    Assert.Equal("DeviceBusCommutation", response.RootElement.GetProperty("ModuleName").GetString());
    Assert.Equal(20, response.RootElement.GetProperty("NumberDevice").GetInt32());
    Assert.Equal(1, response.RootElement.GetProperty("NumberChassis").GetInt32());
    Assert.Equal(expectedAnswer, response.RootElement.GetProperty("Answer").GetString());
  }

  [Theory(DisplayName = "УКШ: строковая команда возвращает ответ в формате прошивки")]
  [InlineData("6.1.2.0", "0")]
  [InlineData("8.17.0.0", "17")]
  [InlineData("41.10.2.1", "1")]
  public async Task RawCommand_ReturnsFirmwareValue(string command, string expected)
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = CreateProtocol();

    Assert.Equal(expected, await protocol.QueryAsync(command));
  }

  [Fact(DisplayName = "УКШ: инициализация возвращает JSON без поля ответа")]
  public async Task Initialize_ReturnsIdentityOnly()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = CreateProtocol();

    using JsonDocument response = JsonDocument.Parse(await protocol.QueryAsync("1.0.0.0"));

    Assert.False(response.RootElement.TryGetProperty("Answer", out _));
  }

  [Theory(DisplayName = "УКШ: некорректная или неизвестная команда возвращает пустой ответ")]
  [InlineData("")]
  [InlineData("1.2.3")]
  [InlineData("abc.def.0.0")]
  [InlineData("99.0.0.0")]
  public async Task InvalidCommand_ReturnsEmpty(string command)
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = CreateProtocol();

    Assert.Empty(await protocol.QueryAsync(command));
  }

  private static DeviceBusCommutationEmulatorProtocol CreateProtocol()
    => new(() => 20, () => 1);
}

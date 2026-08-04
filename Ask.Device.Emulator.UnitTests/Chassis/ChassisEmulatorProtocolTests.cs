using Ask.Device.Emulator.Chassis;

namespace Ask.Device.Emulator.UnitTests.Chassis;

public sealed class ChassisEmulatorProtocolTests
{
  [Fact(DisplayName = "Шасси: команда инициализации возвращает версию контроллера")]
  public async Task Initialize_ReturnsVersion()
  {
    var protocol = new ChassisEmulatorProtocol(() => false);

    string response = await protocol.QueryAsync("1.0.0.0");

    Assert.Equal("1.0.1", response);
  }

  [Fact(DisplayName = "Шасси: включение питания сохраняет включённое состояние")]
  public async Task PowerOn_StoresEnabledState()
  {
    var protocol = new ChassisEmulatorProtocol(() => false);

    Assert.Equal("1", await protocol.QueryAsync("2.1.1.0"));
    Assert.Equal("1", await protocol.QueryAsync("7.0.0.0"));
  }

  [Fact(DisplayName = "Шасси: выключение питания сохраняет выключенное состояние")]
  public async Task PowerOff_StoresDisabledState()
  {
    var protocol = new ChassisEmulatorProtocol(() => false);
    await protocol.QueryAsync("2.1.1.0");

    Assert.Equal("1", await protocol.QueryAsync("2.2.1.0"));
    Assert.Equal("0", await protocol.QueryAsync("7.0.0.0"));
  }

  [Theory(DisplayName = "Шасси: некорректная или неизвестная команда возвращает пустой ответ")]
  [InlineData("")]
  [InlineData("1.2.3")]
  [InlineData("1.-1.0.0")]
  [InlineData("99.0.0.0")]
  public async Task InvalidCommand_ReturnsEmpty(string command)
  {
    var protocol = new ChassisEmulatorProtocol(() => false);

    Assert.Empty(await protocol.QueryAsync(command));
  }

  [Fact(DisplayName = "Шасси: имитация аппаратной ошибки возвращает пустой ответ")]
  public async Task HardwareError_ReturnsEmpty()
  {
    var protocol = new ChassisEmulatorProtocol(() => true);

    Assert.Empty(await protocol.QueryAsync("1.0.0.0"));
  }
}

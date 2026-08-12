using Ask.Device.Emulator.ModuleRelayControl;
using System.Text.Json;

namespace Ask.Device.Emulator.UnitTests.ModuleRelayControl;

public sealed class ModuleRelayControlEmulatorProtocolTests
{
  [Fact(DisplayName = "МКР: инициализация возвращает номер модуля и номер шасси")]
  public async Task Initialize_ReturnsDeviceIdentity()
  {
    var protocol = CreateProtocol();

    using JsonDocument response = await QueryJsonAsync(protocol, "1.0.0.0");

    Assert.Equal("MKR", response.RootElement.GetProperty("ModuleName").GetString());
    Assert.Equal(4, response.RootElement.GetProperty("NumberDevice").GetInt32());
    Assert.Equal(2, response.RootElement.GetProperty("NumberChassis").GetInt32());
  }

  [Fact(DisplayName = "МКР: подключение измерителя отражается в запросе его состояния")]
  public async Task MeterConnection_IsStored()
  {
    var protocol = CreateProtocol();

    await protocol.QueryAsync("5.1");
    using JsonDocument connected = await QueryJsonAsync(protocol, "7");
    await protocol.QueryAsync("5.2");
    using JsonDocument disconnected = await QueryJsonAsync(protocol, "7");

    Assert.Equal("7.1", connected.RootElement.GetProperty("Answer").GetString());
    Assert.Equal("7.2", disconnected.RootElement.GetProperty("Answer").GetString());
  }

  [Theory(DisplayName = "МКР: поддерживаемая команда возвращает подтверждение с исходными параметрами")]
  [InlineData("4.1.2.1", "4.1.2.1")]
  [InlineData("8.10.2.1", "8.10.2.1")]
  [InlineData("9.3.1.0", "9.3.1")]
  [InlineData("81.7.2.0", "81.7.2.0")]
  public async Task SupportedCommand_ReturnsAnswer(string command, string expectedAnswer)
  {
    var protocol = CreateProtocol();

    using JsonDocument response = await QueryJsonAsync(protocol, command);

    Assert.Equal(expectedAnswer, response.RootElement.GetProperty("Answer").GetString());
  }

  [Fact(DisplayName = "МКР: сброс очищает изменённое состояние")]
  public async Task Reset_ClearsState()
  {
    var protocol = CreateProtocol();
    await protocol.QueryAsync("5.1");

    using JsonDocument response = await QueryJsonAsync(protocol, "2.1.0.0");
    using JsonDocument meter = await QueryJsonAsync(protocol, "7");

    Assert.False(response.RootElement.GetProperty("NotDefaultState").GetBoolean());
    Assert.Equal("7.2", meter.RootElement.GetProperty("Answer").GetString());
  }

  [Theory(DisplayName = "МКР: некорректная команда возвращает пустой ответ")]
  [InlineData("")]
  [InlineData("abc")]
  [InlineData("4.0.0.0")]
  [InlineData("99.0.0.0")]
  public async Task InvalidCommand_ReturnsEmpty(string command)
  {
    var protocol = CreateProtocol();

    Assert.Empty(await protocol.QueryAsync(command));
  }

  [Fact(DisplayName = "МКР: имитация аппаратной ошибки возвращает пустой ответ")]
  public async Task HardwareError_ReturnsEmpty()
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(() => 4, () => 2, () => true);

    Assert.Empty(await protocol.QueryAsync("1.0.0.0"));
  }

  [Fact(DisplayName = "МКР: самоконтроль точки без симуляции ошибок проходит успешно")]
  public async Task PointSelfTest_WithoutMeasurementError_ReturnsSuccessfulStages()
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(
      () => 4,
      () => 2,
      () => false,
      () => false);

    using JsonDocument response = await QueryJsonAsync(protocol, "6.1");

    Assert.True(response.RootElement.GetProperty("ConnectPoint").GetBoolean());
    Assert.True(response.RootElement.GetProperty("DisconnectBusA").GetBoolean());
    Assert.True(response.RootElement.GetProperty("DisconnectBusB").GetBoolean());
    Assert.True(response.RootElement.GetProperty("SelfControl").GetBoolean());
  }

  [Theory(DisplayName = "МКР: симуляция ошибки измерения проваливает один этап самоконтроля точки")]
  [InlineData("6.3", "ConnectPoint")]
  [InlineData("6.1", "DisconnectBusA")]
  [InlineData("6.2", "DisconnectBusB")]
  public async Task PointSelfTest_WithMeasurementError_ReturnsFailedStage(
    string command,
    string failedStage)
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(
      () => 4,
      () => 2,
      () => false,
      () => true);

    using JsonDocument response = await QueryJsonAsync(protocol, command);

    Assert.False(response.RootElement.GetProperty(failedStage).GetBoolean());
    Assert.False(response.RootElement.GetProperty("SelfControl").GetBoolean());
  }

  [Fact(DisplayName = "МКР: самоконтроль внешней шины без симуляции ошибок проходит успешно")]
  public async Task ExternalBusSelfTest_WithoutMeasurementError_ReturnsSuccessfulStages()
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(
      () => 4,
      () => 2,
      () => false,
      () => false);

    using JsonDocument response = await QueryJsonAsync(protocol, "10.1");

    Assert.True(response.RootElement.GetProperty("ConnectProtect").GetBoolean());
    Assert.True(response.RootElement.GetProperty("ConnectMain").GetBoolean());
    Assert.Equal(0, response.RootElement.GetProperty("Error").GetInt32());
  }

  [Theory(DisplayName = "МКР: симуляция измерительной ошибки допускает норму и два варианта отказа внешней шины")]
  [InlineData(0, true, true, 0)]
  [InlineData(1, false, true, 1)]
  [InlineData(2, true, false, 1)]
  public async Task ExternalBusSelfTest_WithMeasurementError_ReturnsRandomOutcome(
    int simulationOutcome,
    bool expectedProtect,
    bool expectedMain,
    int expectedError)
  {
    var protocol = new ModuleRelayControlEmulatorProtocol(
      () => 4,
      () => 2,
      () => false,
      () => true,
      () => simulationOutcome);

    using JsonDocument response = await QueryJsonAsync(protocol, "10.1");

    Assert.Equal(expectedProtect, response.RootElement.GetProperty("ConnectProtect").GetBoolean());
    Assert.Equal(expectedMain, response.RootElement.GetProperty("ConnectMain").GetBoolean());
    Assert.Equal(expectedError, response.RootElement.GetProperty("Error").GetInt32());
  }

  private static ModuleRelayControlEmulatorProtocol CreateProtocol()
    => new(() => 4, () => 2, () => false);

  private static async Task<JsonDocument> QueryJsonAsync(
    ModuleRelayControlEmulatorProtocol protocol,
    string command)
    => JsonDocument.Parse(await protocol.QueryAsync(command));
}

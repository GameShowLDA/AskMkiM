using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Engine.UnitTests.Services.Config;

namespace Ask.Engine.UnitTests.DeviceRuntime;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class MultimeterConfigurationCommandTests
{
  [Fact]
  public async Task SetDcVoltageModeAsync_DoesNotWaitForSetCommandResponse()
  {
    var (meter, protocol) = CreateMeter();

    await meter.DcVoltageManager.SetDCVoltageModeAsync();

    Assert.Collection(
      protocol.Calls,
      call =>
      {
        Assert.Equal("CONF:VOLT:DC", call.Command);
        Assert.Equal(0, call.Timeout);
      },
      call =>
      {
        Assert.Equal("FUNC?", call.Command);
        Assert.Equal(meter.DCVCommands.Timeout, call.Timeout);
      });
  }

  [Fact]
  public async Task SetDcVoltageRangeAsync_DoesNotWaitForSetCommandResponse()
  {
    var (meter, protocol) = CreateMeter();
    meter.TypeMode = MultimeterTypeMode.DcVoltage;

    await meter.DcVoltageManager.SetDCVoltageRangeAsync(0.1d);

    var call = Assert.Single(protocol.Calls);
    Assert.Equal("VOLT:DC:RANG 0.1", call.Command);
    Assert.Equal(0, call.Timeout);
  }

  private static (KeysightDevice Meter, ConfigurationProtocolStub Protocol) CreateMeter()
  {
    var protocol = new ConfigurationProtocolStub();
    var meter = new KeysightDevice
    {
      DeviceProtocol = protocol,
    };
    meter.ConnectionInfo.IsConnected = true;

    return (meter, protocol);
  }

  private sealed class ConfigurationProtocolStub : IDeviceProtocol
  {
    public List<(string Command, int Timeout)> Calls { get; } = new();

    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    public Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      Calls.Add((command, timeout));
      return Task.FromResult(command == "FUNC?" ? "VOLT" : string.Empty);
    }
  }
}

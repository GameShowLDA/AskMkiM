using System.Reflection;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;

namespace Ask.Device.Emulator.UnitTests;

public sealed class DeviceProtocolEmulatorTests
{
  [Fact(DisplayName = "Маршрутизация мультиметра: рабочий режим передаёт команду реальному протоколу")]
  public async Task QueryMultimeterAsync_RealMode_UsesRealProtocol()
  {
    using var mode = new TestExecutionMode(idleMode: false);
    var realProtocol = new StubProtocol("REAL");
    IMultimeter device = CreateDevice<IMultimeter>(realProtocol, "Keysight", 2, 3);

    string response = await DeviceProtocolEmulator.QueryMultimeterAsync(device, "*IDN?", "IDLE");

    Assert.Equal("REAL", response);
    Assert.Equal("*IDN?", realProtocol.LastCommand);
  }

  [Fact(DisplayName = "Маршрутизация мультиметра: холостой режим возвращает ответ эмулятора")]
  public async Task QueryMultimeterAsync_IdleMode_UsesEmulator()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var realProtocol = new StubProtocol("REAL");
    IMultimeter device = CreateDevice<IMultimeter>(realProtocol, "Keysight", 2, 3);

    string response = await DeviceProtocolEmulator.QueryMultimeterAsync(device, "*IDN?", "IDLE");

    Assert.Equal("IDLE", response);
    Assert.Null(realProtocol.LastCommand);
  }

  [Fact(DisplayName = "Маршрутизация ППУ: рабочий режим передаёт команду реальному протоколу")]
  public async Task CreateBreakdownTester_RealMode_UsesRealProtocol()
  {
    using var mode = new TestExecutionMode(idleMode: false);
    var realProtocol = new StubProtocol("REAL");
    IBreakdownTester device = CreateDevice<IBreakdownTester>(realProtocol, "GPT", 1, 4);
    IDeviceProtocol protocol = DeviceProtocolEmulator.CreateBreakdownTester(device, realProtocol);

    string response = await protocol.QueryAsync("*IDN?");

    Assert.Equal("REAL", response);
    Assert.Equal("*IDN?", realProtocol.LastCommand);
  }

  [Fact(DisplayName = "Маршрутизация ППУ: холостой режим выполняет команду эмулятором")]
  public async Task CreateBreakdownTester_IdleMode_UsesEmulator()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var realProtocol = new StubProtocol("REAL");
    IBreakdownTester device = CreateDevice<IBreakdownTester>(realProtocol, "GPT", 1, 4);
    IDeviceProtocol protocol = DeviceProtocolEmulator.CreateBreakdownTester(device, realProtocol);

    string response = await protocol.QueryAsync("*IDN?");

    Assert.Contains("GPT-79904", response, StringComparison.Ordinal);
    Assert.Null(realProtocol.LastCommand);
  }

  [Fact(DisplayName = "Фабрика протоколов: пустое устройство отклоняется понятной ошибкой")]
  public void Factories_NullDevice_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => DeviceProtocolEmulator.CreateChassis(null!));
    Assert.Throws<ArgumentNullException>(() => DeviceProtocolEmulator.CreateModuleRelayControl(null!));
    Assert.Throws<ArgumentNullException>(() => DeviceProtocolEmulator.CreateDeviceBusCommutation(null!));
  }

  private static T CreateDevice<T>(IDeviceProtocol protocol, string name, int chassisNumber, int deviceNumber)
    where T : class
  {
    T device = DispatchProxy.Create<T, TestDeviceProxy>();
    var proxy = (TestDeviceProxy)(object)device;
    proxy.Set(nameof(IDevice.DeviceProtocol), protocol);
    proxy.Set(nameof(IDevice.Name), name);
    proxy.Set(nameof(IAttachableDevice.NumberChassis), chassisNumber);
    proxy.Set(nameof(IDevice.Number), deviceNumber);
    return device;
  }

  private sealed class StubProtocol(string response) : IDeviceProtocol
  {
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    public string? LastCommand { get; private set; }

    public Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      LastCommand = command;
      return Task.FromResult(response);
    }
  }
}



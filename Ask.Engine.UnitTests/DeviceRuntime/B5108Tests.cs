using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Moq;
using Ask.Device.Application.Composition;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Runtime.Device;
using Ask.Engine.UnitTests.Services.Config;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ask.Engine.UnitTests.DeviceRuntime;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class B5108Tests
{
  [Fact]
  public void Device_CreatesSeparateCapabilityManagers()
  {
    var device = new B5108();
    object[] managers = [device.VoltageManager, device.CurrentManager, device.OutputManager,
      device.MeasurementManager, device.ConnectableManager];
    Assert.Equal(5, managers.Distinct().Count());
    Assert.Equal(new[] { "VoltageManager", "CurrentManager", "OutputManager", "MeasurementManager", "Transport" },
      managers.Select(manager => manager.GetType().Name));
  }

  [Fact]
  public async Task Composition_UsesSeparateAdaptersAndCommonConnectable()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(true);
      var protocol = new ProtocolStub();
      var device = DeviceApplicationComposer.Compose(new B5108 { DeviceProtocol = protocol });
      Assert.Equal("VoltageManagerAdapter", device.VoltageManager.GetType().Name);
      Assert.Equal("CurrentManagerAdapter", device.CurrentManager.GetType().Name);
      Assert.Equal("OutputManagerAdapter", device.OutputManager.GetType().Name);
      Assert.Equal("MeasurementManagerAdapter", device.MeasurementManager.GetType().Name);
      Assert.Equal("EquipmentTrackingConnectable", device.ConnectableManager.GetType().Name);
      await device.VoltageManager.SetVoltageAsync(0.1m);
      Assert.Equal(0.1m, await device.VoltageManager.GetVoltageAsync());
      Assert.False(await device.OutputManager.GetOutputStateAsync());
      await device.CurrentManager.SetCurrentAsync(0.001m);
      Assert.Equal(0.001m, await device.CurrentManager.GetCurrentAsync());
      Assert.Empty(protocol.Calls);
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Fact]
  public async Task ConcurrentSetpoints_DoNotInterleaveTransactions()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var protocol = new ProtocolStub
      {
        DelayMs = 10,
        Responses = new Queue<string>(["150", "", "0", "0.1", "5", "", "0", "0.001"]),
      };
      var device = new B5108 { DeviceProtocol = protocol };
      await Task.WhenAll(device.VoltageManager.SetVoltageAsync(0.1m), device.CurrentManager.SetCurrentAsync(0.001m));
      Assert.Equal(new[] { "VOLT:LIM?", "VOLT 0.1", "SYST:ERR?", "VOLT?",
        "CURR:LIM?", "CURR 0.001", "SYST:ERR?", "CURR?" }, protocol.Calls);
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Fact]
  public async Task Idle_PreservesSettingsAndOutput_WithoutProtocolCalls()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(true);
      var protocol = new ProtocolStub();
      var device = new B5108 { DeviceProtocol = protocol };
      Assert.True((await device.ConnectableManager.InitializeAsync()).Connect);
      await device.VoltageManager.SetVoltageAsync(0.1m);
      await device.CurrentManager.SetCurrentAsync(0.001m);
      Assert.Equal(0.1m, await device.VoltageManager.GetVoltageAsync());
      Assert.Equal(0.001m, await device.CurrentManager.GetCurrentAsync());
      Assert.Equal(0m, await device.MeasurementManager.MeasureVoltageAsync());
      await device.OutputManager.SetOutputStateAsync(true);
      Assert.True(await device.OutputManager.GetOutputStateAsync());
      Assert.Equal(0.1m, await device.MeasurementManager.MeasureVoltageAsync());
      Assert.Equal(0m, await device.MeasurementManager.MeasureCurrentAsync());
      await device.OutputManager.SetOutputStateAsync(false);
      Assert.False(await device.OutputManager.GetOutputStateAsync());
      Assert.Empty(protocol.Calls);
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Theory]
  [InlineData("-0.1", true)]
  [InlineData("150.1", true)]
  [InlineData("0.01", true)]
  [InlineData("-0.001", false)]
  [InlineData("5.001", false)]
  [InlineData("0.0001", false)]
  public async Task InvalidSetpoint_DoesNotSendCommands(string text, bool voltage)
  {
    var protocol = new ProtocolStub();
    var device = new B5108 { DeviceProtocol = protocol };
    decimal value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => voltage
      ? device.VoltageManager.SetVoltageAsync(value) : device.CurrentManager.SetCurrentAsync(value));
    Assert.Empty(protocol.Calls);
  }

  [Fact]
  public async Task RealSetpoint_ChecksLimitErrorAndReadback()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var protocol = new ProtocolStub { Responses = new Queue<string>(["150", "", "0", "0.1"]) };
      var device = new B5108 { DeviceProtocol = protocol };
      await device.VoltageManager.SetVoltageAsync(0.1m);
      Assert.Equal(new[] { "VOLT:LIM?", "VOLT 0.1", "SYST:ERR?", "VOLT?" }, protocol.Calls);
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Theory]
  [InlineData("0", "0.2")]
  [InlineData("3", "0.1")]
  public async Task Setpoint_RejectsDeviceErrorOrChangedValue(string error, string readback)
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var protocol = new ProtocolStub { Responses = new Queue<string>(["150", "", error, readback]) };
      var device = new B5108 { DeviceProtocol = protocol };
      await Assert.ThrowsAsync<DeviceException>(() => device.VoltageManager.SetVoltageAsync(0.1m));
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Fact]
  public async Task Setpoint_RespectsConfiguredLimitBeforeWriting()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var protocol = new ProtocolStub { Responses = new Queue<string>(["0.5"]) };
      var device = new B5108 { DeviceProtocol = protocol };
      await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => device.CurrentManager.SetCurrentAsync(1));
      Assert.Equal(new[] { "CURR:LIM?" }, protocol.Calls);
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Fact]
  public async Task UserCancellation_StopsQueryAlreadyInProgress()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      using var cancellation = new CancellationTokenSource();
      var user = new Mock<IUserInteractionService>();
      user.Setup(service => service.GetCancellationToken()).Returns(cancellation.Token);
      var protocol = new ProtocolStub { DelayMs = 10000 };
      var device = new B5108 { DeviceProtocol = protocol };
      Task operation = device.MeasurementManager.MeasureVoltageAsync(userMessageService: user.Object);
      await protocol.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
      cancellation.Cancel();
      await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(2)));
      Assert.Single(protocol.Calls);
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Fact]
  public async Task CancelledOperation_DoesNotSendCommands()
  {
    var protocol = new ProtocolStub();
    var device = new B5108 { DeviceProtocol = protocol };
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => device.VoltageManager.GetVoltageAsync(cancellation.Token));
    Assert.Empty(protocol.Calls);
  }

  [Fact]
  public async Task IdleHardwareFailure_ThrowsWithoutNetwork()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(true);
      var protocol = new ProtocolStub();
      var device = new B5108 { DeviceProtocol = protocol, IsHardwareFailureSimulationEnabled = true };
      await Assert.ThrowsAsync<DeviceException>(() => device.OutputManager.SetOutputStateAsync(true));
      Assert.Empty(protocol.Calls);
    }
    finally { ExecutionConfig.SetIdleMode(original); }
  }

  [Fact]
  public async Task RealConnection_ReadsFragmentedIdentity_AndDisconnectsSameSocket()
  {
    bool original = ExecutionConfig.GetIsIdleModeEnabled();
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var device = new B5108(IPAddress.Loopback);
    device.ConnectedProfile.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    try
    {
      ExecutionConfig.SetIdleMode(false);
      Task server = ServeAsync();
      var result = await device.ConnectableManager.ConnectAsync();
      Assert.True(result.Connect);
      Assert.True(device.ConnectionInfo.IsConnected);
      Assert.True(await device.ConnectableManager.DisconnectAsync());
      Assert.False(device.ConnectionInfo.IsConnected);
      Assert.True((await device.ConnectableManager.ConnectAsync()).Connect);
      Assert.True(await device.ConnectableManager.DisconnectAsync());
      await server;
    }
    finally
    {
      (device.DeviceProtocol as IDisposable)?.Dispose();
      ExecutionConfig.SetIdleMode(original);
    }

    async Task ServeAsync()
    {
      for (int attempt = 0; attempt < 2; attempt++)
      {
        using var client = await listener.AcceptTcpClientAsync(timeout.Token);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        Assert.Equal("*IDN?", await reader.ReadLineAsync(timeout.Token));
        await stream.WriteAsync(Encoding.ASCII.GetBytes("KIP,B5-"), timeout.Token);
        await Task.Delay(30, timeout.Token);
        await stream.WriteAsync(Encoding.ASCII.GetBytes("108,240026,1.05\n"), timeout.Token);
        Assert.Equal(0, await stream.ReadAsync(new byte[1], timeout.Token));
      }
    }
  }

  private sealed class ProtocolStub : IDeviceProtocol
  {
    public List<string> Calls { get; } = [];
    public int DelayMs { get; set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Queue<string> Responses { get; set; } = new();
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);
    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0,
      int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Calls.Add(command);
      Started.TrySetResult();
      if (DelayMs > 0) await Task.Delay(DelayMs, cancellationToken);
      return Responses.Dequeue();
    }
  }
}

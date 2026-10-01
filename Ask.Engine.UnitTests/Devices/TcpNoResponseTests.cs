using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Communication.Ethernet.Tcp.Protocols;
using Moq;
using System.Net;
using System.Net.Sockets;

namespace Ask.Engine.UnitTests.Devices;

public sealed class TcpNoResponseTests
{
  [Fact]
  public async Task WatchdogDeadline_PreservesDeviceAndNoResponseKind()
  {
    var device = new Mock<IDevice>();
    device.SetupGet(d => d.Name).Returns("Мультиметр");
    var inner = new Mock<IDeviceProtocol>();
    var pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    inner.Setup(p => p.QueryAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<int>(),
      It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(pending.Task);
    using var watchdog = new Ask.Device.Communication.Common.HardwareWatchdogProtocol(
      inner.Object, "Мультиметр", TimeSpan.FromMilliseconds(50), device.Object);

    var error = await Assert.ThrowsAsync<DeviceNoResponseException>(() => watchdog.QueryAsync("READ?", timeout: 1000));
    Assert.Contains("Мультиметр", error.Message);
    Assert.IsType<TimeoutException>(error.InnerException);
    pending.TrySetResult("late");
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task MissingReplyOrClosedConnection_IsNoResponse(bool closeConnection)
  {
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var device = new Mock<IDevice>();
    device.SetupGet(d => d.Name).Returns("Keysight");
    device.SetupGet(d => d.ConnectionDetails).Returns("127.0.0.1");
    var protocol = new TcpProtocol(device.Object, ((IPEndPoint)listener.LocalEndpoint).Port);
    var pending = protocol.QueryAsync("READ?", timeout: 100);
    using var server = await listener.AcceptTcpClientAsync();
    if (closeConnection) server.Close();

    var error = await Assert.ThrowsAsync<DeviceNoResponseException>(() => pending);
    Assert.Contains("Keysight", error.Message);
    Assert.Contains("READ?", error.Message);
  }
}

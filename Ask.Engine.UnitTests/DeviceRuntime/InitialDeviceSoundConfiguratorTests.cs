using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Runtime.Function.Base.Connected;
using Moq;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public class InitialDeviceSoundConfiguratorTests
{
  [Fact]
  public async Task ApplyOnceAsync_CalledRepeatedly_SendsCommandsOnlyOnce()
  {
    var protocol = new Mock<IDeviceProtocol>();
    protocol
      .Setup(item => item.QueryAsync(
        It.IsAny<string>(),
        It.IsAny<double>(),
        It.IsAny<int>(),
        It.IsAny<int>(),
        It.IsAny<int>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(string.Empty);
    var device = CreateDevice(protocol.Object);
    var configurator = new InitialDeviceSoundConfigurator(device, ["FIRST", "SECOND"]);

    await Task.WhenAll(
      configurator.ApplyOnceAsync(),
      configurator.ApplyOnceAsync(),
      configurator.ApplyOnceAsync());

    protocol.Verify(item => item.QueryAsync(
      "FIRST", 0, 0, 0, 0, It.IsAny<CancellationToken>()), Times.Once);
    protocol.Verify(item => item.QueryAsync(
      "SECOND", 0, 0, 0, 0, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ApplyOnceAsync_WithoutSupportedCommands_DoesNotAccessProtocol()
  {
    var protocol = new Mock<IDeviceProtocol>();
    var configurator = new InitialDeviceSoundConfigurator(CreateDevice(protocol.Object), []);

    await configurator.ApplyOnceAsync();

    protocol.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task ApplyOnceAsync_WhenCommandFails_DoesNotRetryOrPropagateError()
  {
    var protocol = new Mock<IDeviceProtocol>();
    protocol
      .Setup(item => item.QueryAsync(
        It.IsAny<string>(),
        It.IsAny<double>(),
        It.IsAny<int>(),
        It.IsAny<int>(),
        It.IsAny<int>(),
        It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Unsupported command"));
    var configurator = new InitialDeviceSoundConfigurator(CreateDevice(protocol.Object), ["DISABLE"]);

    await configurator.ApplyOnceAsync();
    await configurator.ApplyOnceAsync();

    protocol.Verify(item => item.QueryAsync(
      "DISABLE", 0, 0, 0, 0, It.IsAny<CancellationToken>()), Times.Once);
  }

  private static IDevice CreateDevice(IDeviceProtocol protocol)
  {
    var device = new Mock<IDevice>();
    device.SetupGet(item => item.Name).Returns("Test device");
    device.SetupGet(item => item.DeviceProtocol).Returns(protocol);
    return device.Object;
  }
}

using Ask.Core.Services.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Application.Composition;
using Moq;

namespace Ask.Engine.UnitTests.Services.Devices;

public sealed class EquipmentTrackingConnectableTests
{
  [Fact]
  public async Task FirstConnectionAttemptRegistersDevice()
  {
    var inner = new Mock<IConnectable>();
    inner
      .Setup(x => x.ConnectAsync(null))
      .ReturnsAsync((false, "Нет подключения"));
    var device = new Mock<IDevice>().Object;
    var tracking = new EquipmentTrackingConnectable(device, inner.Object);

    using var session = EquipmentUsageTracker.BeginSession();
    await tracking.ConnectAsync();

    Assert.Equal([device], session.GetUsedDevices());
  }

  [Fact]
  public async Task FailedOperationStillRegistersDevice()
  {
    var inner = new Mock<IConnectable>();
    inner
      .Setup(x => x.InitializeAsync(null))
      .ThrowsAsync(new InvalidOperationException("Ошибка связи"));
    var device = new Mock<IDevice>().Object;
    var tracking = new EquipmentTrackingConnectable(device, inner.Object);

    using var session = EquipmentUsageTracker.BeginSession();
    await Assert.ThrowsAsync<InvalidOperationException>(() => tracking.InitializeAsync());

    Assert.Equal([device], session.GetUsedDevices());
  }
}

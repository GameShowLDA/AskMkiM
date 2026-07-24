using Ask.Core.Services.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Moq;

namespace Ask.Engine.UnitTests.Services.Devices;

public sealed class EquipmentUsageTrackerTests
{
  [Fact]
  public void SessionContainsOnlyRegisteredDevices()
  {
    var used = new Mock<IDevice>().Object;
    var configuredButUnused = new Mock<IDevice>().Object;

    using var session = EquipmentUsageTracker.BeginSession();
    EquipmentUsageTracker.Register(used);

    Assert.Equal([used], session.GetUsedDevices());
    Assert.DoesNotContain(configuredButUnused, session.GetUsedDevices());
  }

  [Fact]
  public void SessionDeduplicatesSameInstanceAndPreservesFirstUseOrder()
  {
    var first = new Mock<IDevice>().Object;
    var second = new Mock<IDevice>().Object;

    using var session = EquipmentUsageTracker.BeginSession();
    EquipmentUsageTracker.Register(first);
    EquipmentUsageTracker.Register(second);
    EquipmentUsageTracker.Register(first);

    Assert.Equal([first, second], session.GetUsedDevices());
  }

  [Fact]
  public async Task SessionFlowsIntoExecutionTask()
  {
    var device = new Mock<IDevice>().Object;

    using var session = EquipmentUsageTracker.BeginSession();
    await Task.Run(() => EquipmentUsageTracker.Register(device));

    Assert.Equal([device], session.GetUsedDevices());
  }

  [Fact]
  public async Task MandatoryResetUsesSessionSnapshotAndSkipsUnusedDevice()
  {
    var usedConnectable = CreateConnectable();
    var unusedConnectable = CreateConnectable();
    var used = CreateDevice(1, usedConnectable.Object);
    var unused = CreateDevice(2, unusedConnectable.Object);

    using var session = EquipmentUsageTracker.BeginSession();
    EquipmentUsageTracker.Register(used);

    using (Ask.Core.Services.UI.EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      await DeviceResetService.ResetDevicesAsync(session.GetUsedDevices());
    }

    usedConnectable.Verify(x => x.ResetAsync(null), Times.Once);
    unusedConnectable.Verify(x => x.ResetAsync(null), Times.Never);
  }

  [Fact]
  public async Task CompletedSessionRetainsSnapshotForMandatoryReset()
  {
    var connectable = CreateConnectable();
    var device = CreateDevice(1, connectable.Object);
    var session = EquipmentUsageTracker.BeginSession();
    EquipmentUsageTracker.Register(device);
    session.Dispose();

    using (Ask.Core.Services.UI.EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      await DeviceResetService.ResetDevicesAsync(session.GetUsedDevices());
    }

    connectable.Verify(x => x.ResetAsync(null), Times.Once);
  }

  private static Mock<IConnectable> CreateConnectable()
  {
    var connectable = new Mock<IConnectable>();
    connectable.Setup(x => x.ResetAsync(null)).ReturnsAsync(true);
    return connectable;
  }

  private static IDevice CreateDevice(int number, IConnectable connectable)
  {
    var device = new Mock<IDevice>();
    device.SetupProperty(x => x.Number, number);
    device.SetupProperty(x => x.Name, $"Устройство {number}");
    device.SetupProperty(x => x.ConnectionDetails, $"connection-{number}");
    device.SetupProperty(x => x.ConnectableManager, connectable);
    return device.Object;
  }
}

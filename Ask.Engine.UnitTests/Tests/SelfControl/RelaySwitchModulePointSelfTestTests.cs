using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Device.Runtime.Function.ModuleRelayControl.SelfCheck;
using Moq;

namespace Ask.Engine.UnitTests.Tests.SelfControl;

public sealed class RelaySwitchModulePointSelfTestTests
{
  [Fact]
  public async Task CheckPointAsync_PreparesChecksAndReleasesModule()
  {
    var connectable = new Mock<IConnectable>();
    connectable
      .Setup(manager => manager.InitializeAsync(null))
      .ReturnsAsync((true, string.Empty));

    var pointManager = new Mock<IPointManager>();
    pointManager
      .Setup(manager => manager.DisconnectingAllPoint(null))
      .ReturnsAsync(true);
    pointManager
      .Setup(manager => manager.CheckPoint(3, null))
      .ReturnsAsync(
        "{\"ModuleName\":\"MKR\",\"NumberDevice\":4,\"NumberChassis\":2," +
        "\"Status\":\"sucsess\",\"NumberPoint\":3,\"ConnectPoint\":true," +
        "\"DisconnectBusA\":true,\"DisconnectBusB\":true,\"SelfControl\":true}");

    var meterManager = new Mock<IMeterManager>();
    meterManager
      .Setup(manager => manager.ConnectMeterAsync(null))
      .ReturnsAsync(true);

    var module = new Mock<IRelaySwitchModule>();
    module.SetupGet(device => device.Name).Returns("Модуль МКР");
    module.SetupGet(device => device.Number).Returns(4);
    module.SetupGet(device => device.NumberChassis).Returns(2);
    module.SetupGet(device => device.PointCount).Returns(8);
    module.SetupGet(device => device.ConnectableManager).Returns(connectable.Object);
    module.SetupGet(device => device.PointManager).Returns(pointManager.Object);
    module.SetupGet(device => device.MeterManager).Returns(meterManager.Object);

    var manager = new SelfTestManager(module.Object);

    bool result = await manager.CheckPointAsync(3);

    Assert.True(result);
    connectable.Verify(device => device.InitializeAsync(null), Times.Once);
    meterManager.Verify(device => device.ConnectMeterAsync(null), Times.Once);
    pointManager.Verify(device => device.CheckPoint(3, null), Times.Once);
    pointManager.Verify(device => device.DisconnectingAllPoint(null), Times.Exactly(2));
  }
}

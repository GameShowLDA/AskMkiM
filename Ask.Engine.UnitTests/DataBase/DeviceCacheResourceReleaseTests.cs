using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.DataBase.Engine.Builder;
using Ask.DataBase.Engine.Services;
using Ask.DataBase.Provider.Services.Devices;
using Moq;

namespace Ask.Engine.UnitTests.DataBase;

public sealed class DeviceCacheResourceReleaseTests
{
  [Fact]
  public void Remove_DisposesCachedDevice()
  {
    var cache = new DeviceCache();
    var device = CreateDisposableDevice(1);
    cache.Set(typeof(IBreakdownTester), 1, device.Object);

    cache.Remove(typeof(IBreakdownTester), 1);

    device.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
  }

  [Fact]
  public void Set_DisposesReplacedDevice()
  {
    var cache = new DeviceCache();
    var previous = CreateDisposableDevice(1);
    var replacement = CreateDisposableDevice(1);
    cache.Set(typeof(IBreakdownTester), 1, previous.Object);

    cache.Set(typeof(IBreakdownTester), 1, replacement.Object);

    previous.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    replacement.As<IDisposable>().Verify(x => x.Dispose(), Times.Never);
  }

  [Fact]
  public void Clear_DisposesAllCachedDevices()
  {
    var cache = new DeviceCache();
    var first = CreateDisposableDevice(1);
    var second = CreateDisposableDevice(2);
    cache.Set(typeof(IBreakdownTester), 1, first.Object);
    cache.Set(typeof(IBreakdownTester), 2, second.Object);

    cache.Clear();

    first.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    second.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
  }

  [Fact]
  public async Task UpdateAsync_WhenSuccessful_DisposesCachedDevice()
  {
    var cache = new DeviceCache();
    var cached = CreateDisposableDevice(1);
    cache.Set(typeof(IBreakdownTester), 1, cached.Object);
    var service = new Mock<BreakdownTesterDtoService>();
    service
      .Setup(x => x.UpdateAsync(It.IsAny<BreakdownTesterDto>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((BreakdownTesterDto dto, CancellationToken _) => dto);
    var engine = new DeviceEngine(cache: cache, breakdownTesterService: service.Object);
    int configurationChangedCount = 0;
    Action<SystemStateEvents.DeviceConfigurationChanged> handler = _ => configurationChangedCount++;
    EventAggregator.Subscribe(handler);

    try
    {
      var result = await engine.UpdateAsync<IBreakdownTester>(CreateBreakdownTester(1));

      cached.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
      Assert.NotSame(cached.Object, result);
      Assert.Equal(1, configurationChangedCount);
    }
    finally
    {
      EventAggregator.Unsubscribe(handler);
    }
  }

  [Theory]
  [InlineData(1, true)]
  [InlineData(2, false)]
  public async Task UpdateAsync_ResetsHardwareFailureSimulationOnlyWhenDeviceNumberChanges(
    int updatedNumber,
    bool expectedSimulationEnabled)
  {
    var storedDto = new BreakdownTesterDto
    {
      Id = 1,
      Number = 1,
      IsHardwareFailureSimulationEnabled = true,
    };
    BreakdownTesterDto? savedDto = null;
    var service = new Mock<BreakdownTesterDtoService>();
    service
      .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
      .ReturnsAsync(storedDto);
    service
      .Setup(x => x.UpdateAsync(It.IsAny<BreakdownTesterDto>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((BreakdownTesterDto dto, CancellationToken _) =>
      {
        savedDto = dto;
        return dto;
      });
    var engine = new DeviceEngine(breakdownTesterService: service.Object);

    await engine.UpdateAsync<IBreakdownTester>(
      CreateBreakdownTester(1, updatedNumber, hardwareFailureSimulationEnabled: true));

    Assert.NotNull(savedDto);
    Assert.Equal(expectedSimulationEnabled, savedDto.IsHardwareFailureSimulationEnabled);
  }

  [Fact]
  public async Task CreateAsync_WhenSuccessful_PublishesDeviceConfigurationChanged()
  {
    var service = new Mock<BreakdownTesterDtoService>();
    service
      .Setup(x => x.CreateAsync(It.IsAny<BreakdownTesterDto>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((BreakdownTesterDto dto, CancellationToken _) =>
      {
        dto.Id = 1;
        return dto;
      });
    var engine = new DeviceEngine(breakdownTesterService: service.Object);
    int configurationChangedCount = 0;
    Action<SystemStateEvents.DeviceConfigurationChanged> handler = _ => configurationChangedCount++;
    EventAggregator.Subscribe(handler);

    try
    {
      await engine.CreateAsync<IBreakdownTester>(CreateBreakdownTester(0));

      Assert.Equal(1, configurationChangedCount);
    }
    finally
    {
      EventAggregator.Unsubscribe(handler);
    }
  }

  [Fact]
  public async Task DeleteByIdAsync_WhenSuccessful_PublishesDeviceConfigurationChanged()
  {
    var service = new Mock<BreakdownTesterDtoService>();
    service
      .Setup(x => x.DeleteByIdAsync(1, It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);
    var engine = new DeviceEngine(breakdownTesterService: service.Object);
    int configurationChangedCount = 0;
    Action<SystemStateEvents.DeviceConfigurationChanged> handler = _ => configurationChangedCount++;
    EventAggregator.Subscribe(handler);

    try
    {
      bool deleted = await engine.DeleteByIdAsync<IBreakdownTester>(1);

      Assert.True(deleted);
      Assert.Equal(1, configurationChangedCount);
    }
    finally
    {
      EventAggregator.Unsubscribe(handler);
    }
  }

  [Fact]
  public async Task UpdateAsync_WhenProviderFails_DisposesAndRemovesCachedDevice()
  {
    var cache = new DeviceCache();
    var cached = CreateDisposableDevice(1);
    cache.Set(typeof(IBreakdownTester), 1, cached.Object);
    var service = new Mock<BreakdownTesterDtoService>();
    service
      .Setup(x => x.UpdateAsync(It.IsAny<BreakdownTesterDto>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Ошибка сохранения"));
    var engine = new DeviceEngine(cache: cache, breakdownTesterService: service.Object);

    await Assert.ThrowsAsync<InvalidOperationException>(
      () => engine.UpdateAsync<IBreakdownTester>(CreateBreakdownTester(1)));

    cached.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    Assert.False(cache.TryGet(typeof(IBreakdownTester), 1, out _));
  }

  [Fact]
  public async Task UpdateAsync_WhenCancelled_DisposesAndRemovesCachedDevice()
  {
    var cache = new DeviceCache();
    var cached = CreateDisposableDevice(1);
    cache.Set(typeof(IBreakdownTester), 1, cached.Object);
    var service = new Mock<BreakdownTesterDtoService>();
    service
      .Setup(x => x.UpdateAsync(It.IsAny<BreakdownTesterDto>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new OperationCanceledException());
    var engine = new DeviceEngine(cache: cache, breakdownTesterService: service.Object);

    await Assert.ThrowsAsync<OperationCanceledException>(
      () => engine.UpdateAsync<IBreakdownTester>(CreateBreakdownTester(1), new CancellationToken(true)));

    cached.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    Assert.False(cache.TryGet(typeof(IBreakdownTester), 1, out _));
  }

  private static Mock<IBreakdownTester> CreateDisposableDevice(int id)
  {
    var device = new Mock<IBreakdownTester>();
    device.SetupProperty(x => x.Id, id);
    device.SetupGet(x => x.Name).Returns("GPT79904");
    device.As<IDisposable>();
    return device;
  }

  private static IBreakdownTester CreateBreakdownTester(
    int id,
    int number = 0,
    bool hardwareFailureSimulationEnabled = false)
  {
    var device = new Mock<IBreakdownTester>();
    device.SetupGet(x => x.Id).Returns(id);
    device.SetupGet(x => x.Number).Returns(number);
    device.SetupGet(x => x.Name).Returns("GPT79904");
    device.SetupGet(x => x.DeviceClass).Returns("Ask.Device.Runtime.Device.GPT79904");
    device.SetupGet(x => x.ConnectionDetails).Returns(string.Empty);
    device.SetupGet(x => x.IsHardwareFailureSimulationEnabled).Returns(hardwareFailureSimulationEnabled);
    return device.Object;
  }
}

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

    var result = await engine.UpdateAsync<IBreakdownTester>(CreateBreakdownTester(1));

    cached.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    Assert.NotSame(cached.Object, result);
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

  private static IBreakdownTester CreateBreakdownTester(int id)
  {
    var device = new Mock<IBreakdownTester>();
    device.SetupGet(x => x.Id).Returns(id);
    device.SetupGet(x => x.Name).Returns("GPT79904");
    device.SetupGet(x => x.DeviceClass).Returns("Ask.Device.Runtime.Device.GPT79904");
    device.SetupGet(x => x.ConnectionDetails).Returns(string.Empty);
    return device.Object;
  }
}

using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.ModuleRelayControl;
using Moq;

namespace Ask.Engine.UnitTests.Devices;

public sealed class NoResponseOperationTests
{
  [Theory]
  [InlineData(false, "Инициализация")]
  [InlineData(true, "Сброс устройства")]
  public async Task Transport_PreservesOriginalOperationInCompactResult(bool reset, string operation)
  {
    var originalIdle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var device = new ModuleRelayControl { Name = "Модуль МКР-300", NumberChassis = 1, Number = 9 };
      var error = new DeviceNoResponseException(device, "1.0.0.0.", 1000);
      var protocol = new Mock<IDeviceProtocol>();
      protocol.Setup(p => p.QueryAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<int>(),
        It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);
      device.DeviceProtocol = protocol.Object;
      using var context = ControlProgramCommandExecutionContext.Enter();
      if (reset)
      {
        Assert.Same(error, await Assert.ThrowsAsync<DeviceNoResponseException>(() => device.ConnectableManager.ResetAsync()));
      }
      else
      {
        var result = await device.ConnectableManager.InitializeAsync();
        Assert.False(result.Connect);
      }

      var message = error.ToMessage();
      Assert.Equal("Модуль МКР-300(1.9)", message.Header);
      Assert.Equal(operation, message.Message);
      Assert.Equal("[НЕТ СВЯЗИ]", message.GetQualityPrefix());
      Assert.Equal(System.Windows.Media.Color.FromRgb(255, 51, 51), message.GetColorMessage());
      Assert.DoesNotContain("1000", message.ToString());
      Assert.DoesNotContain("Отсутствие ответа", message.ToString());
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdle);
    }
  }

  [Theory]
  [InlineData("1.0.0.0.", "Инициализация")]
  [InlineData("11.1.350.32.", "Отключение диапазона точек")]
  public async Task RelayQuery_UsesExistingOperationNames(string command, string operation)
  {
    var originalIdle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var device = new Mock<IRelaySwitchModule>();
      device.SetupGet(d => d.Name).Returns("МКР");
      device.SetupGet(d => d.DeviceProtocol).Returns(CreateProtocol(device.Object).Object);
      var executor = new ModuleRelayControlQueryExecutor(device.Object);

      var error = await Assert.ThrowsAsync<DeviceNoResponseException>(() => executor.QueryAsync(command, 1000));

      Assert.Equal(operation, error.ToMessage().Message);
      Assert.Contains(command, error.Message);
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdle);
    }
  }

  private static Mock<IDeviceProtocol> CreateProtocol(IDevice device)
  {
    var protocol = new Mock<IDeviceProtocol>();
    protocol.Setup(p => p.QueryAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<int>(),
      It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
      .Returns((string command, double _, int timeout, int _, int _, CancellationToken _) =>
        Task.FromException<string>(new DeviceNoResponseException(device, command, timeout)));
    return protocol;
  }
}

using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Engine.UnitTests.Services.Config;
using Moq;

namespace Ask.Engine.UnitTests.DeviceRuntime;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class DeviceBusCommutationPintTests
{
  [Fact]
  public async Task PintConnectionAndDisconnection_ArePublishedToProtocol()
  {
    bool idleMode = ExecutionConfig.GetIsIdleModeEnabled();
    bool connectionInfoVisible = DeviceDisplayConfig.GetConnectionInfoVisibility();
    bool executionParametersVisible = DeviceDisplayConfig.GetExecutionParametersVisibility();

    try
    {
      ExecutionConfig.SetIdleMode(true);
      DeviceDisplayConfig.SetConnectionInfoVisibility(true);
      DeviceDisplayConfig.SetExecutionParametersVisibility(false);

      var output = new Mock<IUserInteractionService>();
      var device = new DeviceBusCommutation { Number = 20, NumberChassis = 1 };

      Assert.True(await device.ConnectorManager.ConnectPINT(SwitchingBusNew.AB2, output.Object));
      Assert.True(await device.ConnectorManager.DisconnectPINT(SwitchingBusNew.AB2, output.Object));

      ShowMessageModel[] messages = output.Invocations
        .Where(invocation => invocation.Method.Name == nameof(IUserInteractionService.ShowMessageAsync))
        .Select(invocation => Assert.IsType<ShowMessageModel>(invocation.Arguments[0]))
        .ToArray();

      Assert.Collection(
        messages,
        message => Assert.Contains("Подключение ПИНТ к шине [AB2]", message.Header),
        message => Assert.Contains("Отключение ПИНТ от шины [AB2]", message.Header));
    }
    finally
    {
      ExecutionConfig.SetIdleMode(idleMode);
      DeviceDisplayConfig.SetConnectionInfoVisibility(connectionInfoVisible);
      DeviceDisplayConfig.SetExecutionParametersVisibility(executionParametersVisible);
    }
  }
}

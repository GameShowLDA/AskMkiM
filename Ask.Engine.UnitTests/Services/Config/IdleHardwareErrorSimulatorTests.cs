using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Device.Application.FunctionAdapters.ModuleVoltageCurrent;
using Ask.Device.Runtime.Device;
using Moq;
using System.Windows;
using System.Windows.Media;

namespace Ask.Engine.UnitTests.Services.Config;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class IdleHardwareErrorSimulatorTests
{
  [Fact]
  public void DeviceSimulationIsOffByDefault()
  {
    Assert.False(new ChassisManagerDto().IsHardwareFailureSimulationEnabled);
  }

  [Fact]
  public async Task HardwareSimulationIsDisabledOutsideIdleMode()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = false,
      });

      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(true));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationIsDisabledForUnselectedDevice()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
      });

      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(false));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationAlwaysFailsForSelectedDeviceInIdleMode()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        IsErrorSimulationMode = false,
      });

      Assert.True(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(true));
      Assert.True(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(true));
      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(false));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task SimulatedFailureUsesExistingOperationSpecificMessage()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      EnsureApplicationWithResources();
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
      });

      var messages = new List<ShowMessageModel>();
      int lastLineNumber = 0;
      var interaction = new Mock<IUserInteractionService>();
      interaction
        .Setup(service => service.GetCancellationToken())
        .Returns(CancellationToken.None);
      interaction
        .Setup(service => service.GetLastLineNumber())
        .Returns(() => lastLineNumber);
      interaction
        .Setup(service => service.WaitUserActionAsync(
          It.IsAny<bool>(),
          It.IsAny<bool>(),
          It.IsAny<bool>()))
        .ReturnsAsync(UserAction.None);
      interaction
        .Setup(service => service.ShowMessageAsync(
          It.IsAny<ShowMessageModel>(),
          It.IsAny<bool>(),
          It.IsAny<bool>(),
          It.IsAny<bool>(),
          It.IsAny<bool>(),
          It.IsAny<string>(),
          It.IsAny<string>(),
          It.IsAny<int>()))
        .Callback<ShowMessageModel, bool, bool, bool, bool, string, string, int>(
          (message, _, _, _, _, _, _, _) =>
          {
            messages.Add(message);
            lastLineNumber++;
          })
        .Returns(Task.CompletedTask);

      var device = new ModuleVoltageCurrentSource
      {
        IsHardwareFailureSimulationEnabled = true,
      };
      var adapter = new VoltageManagerAdapter(device);

      DeviceException exception = await Assert.ThrowsAsync<DeviceException>(
        () => adapter.SetVoltageLevelAsync(5, 250, interaction.Object));

      Assert.Equal("Ошибка установки напряжения 5.250 В", exception.Message);
      ShowMessageModel protocolMessage = Assert.Single(messages);
      Assert.Equal(exception.Message, protocolMessage.Message);
      Assert.DoesNotContain("симуляц", protocolMessage.Message, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("холост", protocolMessage.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  private static void EnsureApplicationWithResources()
  {
    var application = Application.Current ?? new Application();
    application.Resources["TestsProtocolMessageSuccesForeground"] = new SolidColorBrush(Colors.Green);
    application.Resources["TestsProtocolMessageErrorForeground"] = new SolidColorBrush(Colors.Red);
  }
}

[CollectionDefinition(nameof(ExecutionConfigCollection), DisableParallelization = true)]
public sealed class ExecutionConfigCollection;

using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Settings;

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
}

[CollectionDefinition(nameof(ExecutionConfigCollection), DisableParallelization = true)]
public sealed class ExecutionConfigCollection;

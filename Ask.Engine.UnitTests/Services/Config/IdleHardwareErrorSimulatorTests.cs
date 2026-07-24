using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Settings;

namespace Ask.Engine.UnitTests.Services.Config;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class IdleHardwareErrorSimulatorTests
{
  [Fact]
  public void HardwareSimulationIsOffByDefault()
  {
    Assert.False(new SettingsExecutionDto().IsHardwareErrorSimulationMode);
  }

  [Fact]
  public void FailureRollMatchesExactlyOneOutcomeOfTwo()
  {
    Assert.True(IdleHardwareErrorSimulator.IsFailureRoll(0));
    Assert.False(IdleHardwareErrorSimulator.IsFailureRoll(1));
  }

  [Fact]
  public async Task SettingsRemainIndependentDuringRoundTrip()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        IsErrorSimulationMode = true,
        IsHardwareErrorSimulationMode = false,
      });

      SettingsExecutionDto measurementOnly = await ExecutionConfig.GetExecitonModel();
      Assert.True(measurementOnly.IsErrorSimulationMode);
      Assert.False(measurementOnly.IsHardwareErrorSimulationMode);

      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        IsErrorSimulationMode = false,
        IsHardwareErrorSimulationMode = true,
      });

      SettingsExecutionDto hardwareOnly = await ExecutionConfig.GetExecitonModel();
      Assert.False(hardwareOnly.IsErrorSimulationMode);
      Assert.True(hardwareOnly.IsHardwareErrorSimulationMode);
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
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
        IsHardwareErrorSimulationMode = true,
      });

      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(0));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationIsDisabledWhenSettingIsOff()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        IsHardwareErrorSimulationMode = false,
      });

      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(0));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task HardwareSimulationUsesRollWhenIdleAndSettingAreEnabled()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();

    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        IsErrorSimulationMode = false,
        IsHardwareErrorSimulationMode = true,
      });

      Assert.True(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(0));
      Assert.False(IdleHardwareErrorSimulator.ShouldSimulateHardwareError(1));
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }
}

[CollectionDefinition(nameof(ExecutionConfigCollection), DisableParallelization = true)]
public sealed class ExecutionConfigCollection;

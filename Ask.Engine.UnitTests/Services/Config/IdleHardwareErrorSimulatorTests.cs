using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Engine.UnitTests.Services.Config;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class IdleHardwareErrorSimulatorTests
{
  [Fact]
  public void HardwareSimulationIsOffByDefault()
  {
    Assert.False(new SettingsExecutionDto().IsHardwareErrorSimulationMode);
    Assert.Equal(
      TypeErroneousMeasurement.None,
      new SettingsExecutionDto().ErroneousMeasurementType);
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
        ErroneousMeasurementType = TypeErroneousMeasurement.Rnd,
        IsHardwareErrorSimulationMode = false,
      });

      SettingsExecutionDto measurementOnly = await ExecutionConfig.GetExecitonModel();
      Assert.Equal(TypeErroneousMeasurement.Rnd, measurementOnly.ErroneousMeasurementType);
      Assert.False(measurementOnly.IsHardwareErrorSimulationMode);

      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        ErroneousMeasurementType = TypeErroneousMeasurement.None,
        IsHardwareErrorSimulationMode = true,
      });

      SettingsExecutionDto hardwareOnly = await ExecutionConfig.GetExecitonModel();
      Assert.Equal(TypeErroneousMeasurement.None, hardwareOnly.ErroneousMeasurementType);
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
        ErroneousMeasurementType = TypeErroneousMeasurement.None,
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

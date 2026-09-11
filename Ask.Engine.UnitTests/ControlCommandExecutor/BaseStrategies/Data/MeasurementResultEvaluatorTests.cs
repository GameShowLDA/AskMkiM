using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.UnitTests.Services.Config;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.BaseStrategies.Data;

[Collection(nameof(ExecutionConfigCollection))]
public class MeasurementResultEvaluatorTests
{
  [Theory]
  [InlineData(TypeErroneousMeasurement.Low)]
  [InlineData(TypeErroneousMeasurement.High)]
  [InlineData(TypeErroneousMeasurement.Rnd)]
  public async Task Evaluate_IdleErrorMode_ReturnsValueOutsideAllowedRange(
    TypeErroneousMeasurement type)
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();
    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        ErroneousMeasurementType = type,
      });
      var range = new MeasurementRange(150, 100, 200);

      var result = MeasurementResultEvaluator.Evaluate(range);

      Assert.False(result.IsSuccessful);
      if (type == TypeErroneousMeasurement.Low)
      {
        Assert.True(result.Value < range.LowerBound);
      }
      else if (type == TypeErroneousMeasurement.High)
      {
        Assert.True(result.Value > range.UpperBound);
      }
      else
      {
        Assert.True(result.Value < range.LowerBound || result.Value > range.UpperBound);
      }
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public async Task Evaluate_IdleNoneMode_KeepsTargetValue()
  {
    SettingsExecutionDto original = await ExecutionConfig.GetExecitonModel();
    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        IdleModeExecution = true,
        ErroneousMeasurementType = TypeErroneousMeasurement.None,
      });
      var range = new MeasurementRange(150, 100, 200);

      var result = MeasurementResultEvaluator.Evaluate(range);

      Assert.True(result.IsSuccessful);
      Assert.Equal(150, result.Value);
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public void Evaluate_WithoutUpperBound_AcceptsValueAboveLowerBound()
  {
    var originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var range = new MeasurementRange(60_001, 100, -1);

      var result = MeasurementResultEvaluator.Evaluate(range);

      Assert.True(result.IsSuccessful);
      Assert.Equal(60_001, result.Value);
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }

  [Fact]
  public void Evaluate_WithoutUpperBound_RejectsValueBelowLowerBound()
  {
    var originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var range = new MeasurementRange(99, 100, -1);

      var result = MeasurementResultEvaluator.Evaluate(range);

      Assert.False(result.IsSuccessful);
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }

  [Theory]
  [InlineData(100, true)]
  [InlineData(150, true)]
  [InlineData(200, true)]
  [InlineData(99, false)]
  [InlineData(201, false)]
  public void Evaluate_WithBothBounds_ChecksLowerAndUpperBound(double value, bool expected)
  {
    var originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var range = new MeasurementRange(value, 100, 200);

      var result = MeasurementResultEvaluator.Evaluate(range);

      Assert.Equal(expected, result.IsSuccessful);
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }

  [Fact]
  public void Evaluate_WithoutUpperBound_RejectsUnexpectedOverload()
  {
    var originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var range = new MeasurementRange(double.PositiveInfinity, 100, -1);

      var result = MeasurementResultEvaluator.Evaluate(range);

      Assert.False(result.IsSuccessful);
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }

  [Fact]
  public void Evaluate_ExpectedOverload_AcceptsOverload()
  {
    var originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var range = new MeasurementRange(double.PositiveInfinity, 100, -1);

      var result = MeasurementResultEvaluator.Evaluate(range, isOverloadExpected: true);

      Assert.True(result.IsSuccessful);
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }

  [Theory]
  [InlineData(101, 100, true)]
  [InlineData(100, 100, false)]
  [InlineData(99, 100, false)]
  [InlineData(double.PositiveInfinity, 100, true)]
  public void EvaluateDisconnection_ChecksValueStrictlyAboveThreshold(
    double value,
    double threshold,
    bool expected)
  {
    var result = MeasurementResultEvaluator.EvaluateDisconnection(value, threshold);

    Assert.Equal(expected, result.IsSuccessful);
    Assert.Equal(value, result.Value);
  }
}

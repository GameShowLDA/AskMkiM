using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.UnitTests.Services.Config;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.BaseStrategies.Data;

[Collection(nameof(ExecutionConfigCollection))]
public class MeasurementResultEvaluatorTests
{
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
}

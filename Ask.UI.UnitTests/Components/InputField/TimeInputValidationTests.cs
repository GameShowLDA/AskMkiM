using Ask.Core.Services.Errors.Models;
using Ask.UI.Components.InputField.Controls;

namespace Ask.UI.UnitTests.Components.InputField
{
  public sealed class TimeInputValidationTests
  {
    [Fact]
    public void Validate_ReturnsExecutionTimeErrorForInvalidValue()
    {
      RunInSta(() =>
      {
        var control = new TimeInput
        {
          Role = TimeInputRole.ExecutionTime,
          Text = "invalid"
        };

        var error = control.Validate();

        Assert.NotNull(error);
        Assert.Equal(ErrorCode.Metrology_Validation_InvalidTime, error.Code);
        Assert.Contains("Время выполнения", error.Description);
      });
    }

    [Fact]
    public void Validate_ReturnsRampTimeErrorForInvalidValue()
    {
      RunInSta(() =>
      {
        var control = new TimeInput
        {
          Role = TimeInputRole.RampTime,
          Text = "invalid"
        };

        var error = control.Validate();

        Assert.NotNull(error);
        Assert.Equal(ErrorCode.Metrology_Validation_InvalidTime, error.Code);
        Assert.Contains("Время нарастания", error.Description);
      });
    }

    [Theory]
    [InlineData("1")]
    [InlineData("60")]
    public void Validate_ReturnsNullForValidExecutionTime(string value)
    {
      RunInSta(() =>
      {
        var control = new TimeInput
        {
          Role = TimeInputRole.ExecutionTime,
          Text = value
        };

        var error = control.Validate();

        Assert.Null(error);
      });
    }

    [Theory]
    [InlineData("0")]
    [InlineData("61")]
    [InlineData("1.5")]
    public void Validate_ReturnsErrorForInvalidExecutionTime(string value)
    {
      RunInSta(() =>
      {
        var control = new TimeInput
        {
          Role = TimeInputRole.ExecutionTime,
          Text = value
        };

        Assert.NotNull(control.Validate());
      });
    }

    [Theory]
    [InlineData("0.1")]
    [InlineData("0,1")]
    [InlineData("10")]
    public void Validate_ReturnsNullForValidRampTime(string value)
    {
      RunInSta(() =>
      {
        var control = new TimeInput
        {
          Role = TimeInputRole.RampTime,
          Text = value
        };

        var error = control.Validate();

        Assert.Null(error);
      });
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.09")]
    [InlineData("10.1")]
    public void Validate_ReturnsErrorForRampTimeOutsideRange(string value)
    {
      RunInSta(() =>
      {
        var control = new TimeInput
        {
          Role = TimeInputRole.RampTime,
          Text = value
        };

        Assert.NotNull(control.Validate());
      });
    }

    private static void RunInSta(Action action)
    {
      Exception? exception = null;
      var thread = new Thread(() =>
      {
        try
        {
          action();
        }
        catch (Exception ex)
        {
          exception = ex;
        }
      });

      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();

      if (exception != null)
        throw new InvalidOperationException(exception.ToString(), exception);
    }
  }
}

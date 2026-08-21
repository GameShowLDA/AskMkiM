using Ask.Core.Services.Errors.Models;
using Ask.UI.Components.InputField.Controls;

namespace Ask.UI.UnitTests.Components.InputField
{
  public sealed class ElectricalInputValidationTests
  {
    [Fact]
    public void Validate_ReturnsParameterErrorForInvalidValue()
    {
      RunInSta(() =>
      {
        var control = new ElectricalInput
        {
          Role = ElectricalInputRole.Parameter,
          Text = "invalid"
        };

        var error = control.Validate();

        Assert.NotNull(error);
        Assert.Equal(ErrorCode.Metrology_Validation_InvalidParameter, error.Code);
      });
    }

    [Fact]
    public void Validate_ReturnsVoltageErrorForInvalidValue()
    {
      RunInSta(() =>
      {
        var control = new ElectricalInput
        {
          Role = ElectricalInputRole.Voltage,
          Text = "invalid"
        };

        var error = control.Validate();

        Assert.NotNull(error);
        Assert.Equal(ErrorCode.Metrology_Validation_InvalidVoltage, error.Code);
      });
    }

    [Fact]
    public void Validate_ReturnsVoltageErrorForFractionalValue()
    {
      RunInSta(() =>
      {
        var control = new ElectricalInput
        {
          Role = ElectricalInputRole.Voltage,
          Text = "10.5"
        };

        var error = control.Validate();

        Assert.NotNull(error);
        Assert.Equal(ErrorCode.Metrology_Validation_InvalidVoltage, error.Code);
      });
    }

    [Fact]
    public void Validate_ReturnsNullForIntegerVoltage()
    {
      RunInSta(() =>
      {
        var control = new ElectricalInput
        {
          Role = ElectricalInputRole.Voltage,
          Text = "10"
        };

        var error = control.Validate();

        Assert.Null(error);
      });
    }

    [Fact]
    public void Validate_ReturnsNullForValidParameter()
    {
      RunInSta(() =>
      {
        var control = new ElectricalInput
        {
          Role = ElectricalInputRole.Parameter,
          Text = "10.5"
        };

        var error = control.Validate();

        Assert.Null(error);
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

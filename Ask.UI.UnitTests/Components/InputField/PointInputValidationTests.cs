using Ask.Core.Services.Errors.Models;
using Ask.UI.Components.InputField.Controls;

namespace Ask.UI.UnitTests.Components.InputField
{
  public sealed class PointInputValidationTests
  {
    [Fact]
    public void Validate_ReturnsFirstPointErrorForInvalidFirstPoint()
    {
      RunInSta(() =>
      {
        var control = new PointInput
        {
          Role = PointInputRole.First,
          Text = "invalid"
        };

        var error = control.Validate();

        Assert.NotNull(error);
        Assert.Equal(ErrorCode.Metrology_Validation_InvalidFirstPointFormat, error.Code);
      });
    }

    [Fact]
    public void Validate_ReturnsSecondPointErrorForInvalidSecondPoint()
    {
      RunInSta(() =>
      {
        var control = new PointInput
        {
          Role = PointInputRole.Second,
          Text = "1.2"
        };

        var error = control.Validate();

        Assert.NotNull(error);
        Assert.Equal(ErrorCode.Metrology_Validation_InvalidSecondPointFormat, error.Code);
      });
    }

    [Fact]
    public void Validate_ReturnsNullForValidPoint()
    {
      RunInSta(() =>
      {
        var control = new PointInput
        {
          Role = PointInputRole.First,
          Text = "1.6.1"
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

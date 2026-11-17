using Ask.UI.Shared.Controls;
using UI.Controls.Settings.Configuration;
using System.Windows;

namespace Ask.UI.UnitTests;

public sealed class BreakdownVoltageUiTests
{
  [Fact]
  public void LegacyZeroBoundsUseDeviceDefaults()
  {
    var model = DeviceConfigurationService.ParseConfigurationFile("""
      { "BreakdownTesters": [ { "AcwMaxVoltage": 0, "DcwMaxVoltage": 0, "SiMaxVoltage": 0, "IRMinVoltage": 0 } ] }
      """);
    var tester = Assert.Single(model.BreakdownTesters);
    Assert.Equal(700, tester.AcwVoltageRange.MaxVoltage);
    Assert.Equal(1000, tester.DcwVoltageRange.MaxVoltage);
    Assert.Equal(50, tester.IrVoltageRange.MinVoltage);
    Assert.Equal(1000, tester.IrVoltageRange.MaxVoltage);
  }

  [Fact]
  public void LegacyConfigurationImportsBoundsWithNewStepAndException()
  {
    var model = DeviceConfigurationService.ParseConfigurationFile("""
      { "Version": 1, "BreakdownTesters": [
        { "AcwMaxVoltage": 650, "DcwMaxVoltage": 900, "SiMaxVoltage": 950, "IRMinVoltage": 100 }
      ] }
      """);
    var tester = Assert.Single(model.BreakdownTesters);
    Assert.Equal(650, tester.AcwVoltageRange.MaxVoltage);
    Assert.Equal(2, tester.AcwVoltageRange.Step);
    Assert.Equal(900, tester.DcwVoltageRange.MaxVoltage);
    Assert.Equal(100, tester.IrVoltageRange.MinVoltage);
    Assert.Equal(950, tester.IrVoltageRange.MaxVoltage);
    Assert.Equal(new double[] { 125 }, tester.IrVoltageRange.Exceptions);
  }

  [Fact]
  public void CurrentConfigurationPreservesCustomRanges()
  {
    var model = DeviceConfigurationService.ParseConfigurationFile("""
      { "Version": 2, "BreakdownTesters": [
        { "IrVoltageRange": { "MinVoltage": 50, "MaxVoltage": 900, "Step": 100, "Exceptions": [125,175] } }
      ] }
      """);
    var range = Assert.Single(model.BreakdownTesters).IrVoltageRange;
    Assert.Equal(100, range.Step);
    Assert.Equal(900, range.MaxVoltage);
    Assert.Equal(new double[] { 125, 175 }, range.Exceptions);
  }

  [Fact]
  public void NumericControlKeepsExceptionAndIncludesItInOptions()
  {
    Exception? error = null;
    var thread = new Thread(() =>
    {
      try
      {
        var control = new NumericComboBox
          { Minimum = 50, Maximum = 1000, Increment = 50, Exceptions = new double[] { 125 }, Value = 125, Unit = "В" };
        control.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        Assert.Equal(125, control.Value);
        Assert.Contains(control.Items.Cast<object>(), option => option.ToString() == "125 В");
        control.Value = 126;
        Assert.Equal(150, control.Value);
      }
      catch (Exception ex) { error = ex; }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (error != null)
      System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
  }
}

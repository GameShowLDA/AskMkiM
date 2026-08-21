using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.Tests.Base;

namespace Ask.Engine.UnitTests.Tests.Base
{
  public sealed class UIValidationHelperProtocolTests
  {
    [Fact]
    public void BuildMetrologyInputParameters_ForKc_ContainsPointsAndResistance()
    {
      var data = CreateData();

      var parameters = UIValidationHelper.BuildMetrologyInputParameters(
        MeasurementTypeCommand.KC,
        data,
        timeCheck: false,
        voltageCheck: false,
        timeRampCheck: false,
        busCheck: false,
        pairBusCheck: false);

      Assert.Collection(
        parameters,
        parameter => Assert.Equal(("Первая точка", "1.2.3"), parameter),
        parameter => Assert.Equal(("Вторая точка", "1.2.4"), parameter),
        parameter =>
        {
          Assert.Equal("Заданное значение сопротивления", parameter.Header);
          Assert.Equal("10 Ом", parameter.Value);
        });
    }

    [Fact]
    public void BuildMetrologyInputParameters_ForPi_ContainsActiveAdditionalParameters()
    {
      var data = CreateData();
      data.Time = 30;
      data.RampTime = 0.5;
      data.Voltage = 100;

      var parameters = UIValidationHelper.BuildMetrologyInputParameters(
        MeasurementTypeCommand.PI_ACW,
        data,
        timeCheck: true,
        voltageCheck: true,
        timeRampCheck: true,
        busCheck: false,
        pairBusCheck: false);

      Assert.Contains(parameters, parameter => parameter.Header == "Время выполнения" && parameter.Value == "30 с");
      Assert.Contains(
        parameters,
        parameter => parameter.Header == "Время нарастания" &&
                     parameter.Value == MeasurementValueFormatter.FormatWithUnit(0.5, "с"));
      Assert.Contains(parameters, parameter => parameter.Header == "Напряжение" && parameter.Value == "100 В");
    }

    private static UIValidationHelper.DataModel CreateData() =>
      new(
        new PointModel
        {
          DeviceNumber = 1,
          ModuleNumber = 2,
          PointNumber = 3
        },
        new PointModel
        {
          DeviceNumber = 1,
          ModuleNumber = 2,
          PointNumber = 4
        },
        10);
  }
}

using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.Tests.Base;

namespace Ask.Engine.UnitTests.Tests.Base
{
  public sealed class UIValidationHelperProtocolTests
  {
    [Fact]
    public void BuildMetrologyInputMessages_ForKc_ContainsCommandPointsAndResistance()
    {
      var data = CreateData();

      var messages = UIValidationHelper.BuildMetrologyInputMessages(
        "Режим КС",
        MeasurementTypeCommand.KC,
        data,
        timeCheck: false,
        voltageCheck: false,
        timeRampCheck: false,
        busCheck: false,
        pairBusCheck: false);

      Assert.Collection(
        messages,
        message =>
        {
          Assert.Equal("Запуск \"Режим КС\"", message.Header);
          Assert.Equal(ShowMessageModel.MessageType.Info, message.Status);
        },
        message => Assert.Equal("1.2.3", message.Message),
        message => Assert.Equal("1.2.4", message.Message),
        message =>
        {
          Assert.Equal("Заданное значение сопротивления", message.Header);
          Assert.Equal("10 Ом", message.Message);
        });
    }

    [Fact]
    public void BuildMetrologyInputMessages_ForPi_ContainsActiveAdditionalParameters()
    {
      var data = CreateData();
      data.Time = 30;
      data.RampTime = 0.5;
      data.Voltage = 100;

      var messages = UIValidationHelper.BuildMetrologyInputMessages(
        "Режим ПИ ACW",
        MeasurementTypeCommand.PI_ACW,
        data,
        timeCheck: true,
        voltageCheck: true,
        timeRampCheck: true,
        busCheck: false,
        pairBusCheck: false);

      Assert.Contains(messages, message => message.Header == "Время выполнения" && message.Message == "30 с");
      Assert.Contains(
        messages,
        message => message.Header == "Время нарастания" &&
                   message.Message == MeasurementValueFormatter.FormatWithUnit(0.5, "с"));
      Assert.Contains(messages, message => message.Header == "Напряжение" && message.Message == "100 В");
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

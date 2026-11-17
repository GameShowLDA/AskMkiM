using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.ResponseProcessor.PowerSupply;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public sealed class PowerSupplyResponseProcessorTests
{
  [Theory]
  [InlineData("0", PowerSupplyRegulationMode.OutputDisabled)]
  [InlineData("1", PowerSupplyRegulationMode.ConstantVoltage)]
  [InlineData("3", PowerSupplyRegulationMode.ConstantCurrent)]
  [InlineData("5", PowerSupplyRegulationMode.ConstantVoltage)]
  [InlineData("7", PowerSupplyRegulationMode.ConstantCurrent)]
  public void ParseRegulationMode_DecodesOperationRegister(
    string response,
    PowerSupplyRegulationMode expected)
  {
    Assert.Equal(expected, PowerSupplyResponseProcessor.ParseRegulationMode(response));
  }

  [Theory]
  [InlineData("-1")]
  [InlineData("1.5")]
  [InlineData("invalid")]
  public void ParseRegulationMode_InvalidRegister_Throws(string response)
  {
    Assert.Throws<DeviceException>(() => PowerSupplyResponseProcessor.ParseRegulationMode(response));
  }
}

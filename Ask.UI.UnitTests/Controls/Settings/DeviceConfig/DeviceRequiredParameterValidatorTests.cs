using UI.Controls.Settings.DeviceConfig.Base;

namespace Ask.UI.UnitTests.Controls.Settings.DeviceConfig;

public class DeviceRequiredParameterValidatorTests
{
  [Theory]
  [InlineData("IP", true, "IP")]
  [InlineData("COM", true, "COM")]
  [InlineData("USB", true, "USB")]
  [InlineData("Выбор типа подключения:", false, "")]
  [InlineData("Выбор типа подключения:", true, "")]
  [InlineData("", true, "")]
  [InlineData(null, true, "")]
  public void NormalizeConnectionType_ReturnsOnlyEnabledSupportedTypes(
    string? content,
    bool isEnabled,
    string expected)
  {
    string actual = DeviceRequiredParameterValidator.NormalizeConnectionType(content, isEnabled);

    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData(192, 168, 1, 10, true)]
  [InlineData(0, 0, 0, 0, true)]
  [InlineData(255, 255, 255, 255, true)]
  [InlineData(-1, 168, 1, 10, false)]
  [InlineData(192, 168, 1, 256, false)]
  public void IsValidIpAddress_ChecksAllOctets(int part1, int part2, int part3, int part4, bool expected)
  {
    bool actual = DeviceRequiredParameterValidator.IsValidIpAddress(part1, part2, part3, part4);

    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData("0", true)]
  [InlineData("1,5", true)]
  [InlineData("2.5", true)]
  [InlineData("", false)]
  [InlineData("-1", false)]
  [InlineData("abc", false)]
  public void IsNonNegativeNumber_RequiresFilledValidValue(string text, bool expected)
  {
    bool actual = DeviceRequiredParameterValidator.IsNonNegativeNumber(text);

    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData("1", true)]
  [InlineData("1,5", true)]
  [InlineData("0", false)]
  [InlineData("", false)]
  public void IsPositiveNumber_RequiresValueAboveZero(string text, bool expected)
  {
    bool actual = DeviceRequiredParameterValidator.IsPositiveNumber(text);

    Assert.Equal(expected, actual);
  }
}

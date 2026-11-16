using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Vi;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Device.Runtime.Device;
using Ask.Engine.Equipment;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Vi;

public class ViCommandParserTests
{
  private static readonly ViPowerSupplyLimits DefaultSupply = new(
    PintNumber: 3,
    DeviceNumber: 17,
    NumberChassis: 1,
    MinVoltage: 0m,
    MaxVoltage: 150m,
    VoltageStep: 0.1m,
    MinCurrent: 0m,
    MaxCurrent: 5m,
    CurrentStep: 0.001m);

  [Fact]
  public void CanParse_ViMnemonic_ReturnsTrue()
  {
    var parser = CreateParser(DefaultSupply);

    Assert.True(parser.CanParse(new MnemonicIdentifier("ВИ")));
    Assert.False(parser.CanParse(new MnemonicIdentifier("ВШ")));
  }

  [Fact]
  public void Parse_NumberOnly_BuildsDisconnectOperation()
  {
    var model = Parse("320 ВИ *3*");

    Assert.Equal(ViOperation.DisconnectAndRelease, model.Operation);
    Assert.Equal(3, model.PowerSupplyNumber);
    Assert.Equal(17, model.PowerSupplyDeviceNumber);
    Assert.Equal(1, model.PowerSupplyChassisNumber);
    Assert.Empty(model.Errors);
  }

  [Theory]
  [InlineData("320 ВИ *3, 24В, 100мА*")]
  [InlineData("320 ВИ *3, 100МА, 24В*")]
  public void Parse_VoltageAndCurrentInEitherOrder_BuildsConfigurationOperation(string source)
  {
    var model = Parse(source);

    Assert.Equal(ViOperation.ConfigureWithoutBuses, model.Operation);
    Assert.Equal(24m, model.Voltage);
    Assert.Equal(0.1m, model.Current);
    Assert.Empty(model.Errors);
  }

  [Fact]
  public void Parse_BusPairAndFastCondition_BuildsConnectOperation()
  {
    var model = Parse("310 ВИ Б *3, 400МА, 24В, +А2, -В2*");

    Assert.Equal(ViOperation.ConfigureAndConnect, model.Operation);
    Assert.Equal(SwitchingBus.A2, model.PositiveBus);
    Assert.Equal(SwitchingBus.B2, model.NegativeBus);
    Assert.Contains("Б", model.AlgorithmKey);
    Assert.Empty(model.Errors);
  }

  [Fact]
  public void Parse_FastConditionInsideParameterBlock_IsRejected()
  {
    var model = Parse("310 ВИ *3, Б, 400мА, 24В*");

    Assert.Equal(ViOperation.Unknown, model.Operation);
    Assert.NotEmpty(model.Errors);
  }

  [Fact]
  public void BuildFormattedSourceText_FastCondition_RemainsParseable()
  {
    var parser = CreateParser(DefaultSupply);
    var manager = new CommandTranslationManager(new[] { parser });

    string formatted = manager.BuildFormattedSourceText("310 ВИ Б *3, 24В, 100мА, +A2, -B2*");
    var reparsed = Assert.IsType<ViCommandModel>(parser.Parse(
      "310", "ВИ", 1, new List<string> { formatted }));

    Assert.Contains("Б", reparsed.AlgorithmKey);
    Assert.Equal(ViOperation.ConfigureAndConnect, reparsed.Operation);
    Assert.Empty(reparsed.Errors);
  }

  [Fact]
  public void Parse_ValueOutsideConfiguredRange_AddsEquipmentLimitErrors()
  {
    var model = Parse("310 ВИ *3, 151В, 5.001А*");

    Assert.Contains(model.Errors, error => error.Code == ErrorCode.Vi_InvalidVoltage);
    Assert.Contains(model.Errors, error => error.Code == ErrorCode.Vi_InvalidCurrent);
  }

  [Fact]
  public void Parse_ValueNotMatchingConfiguredStep_AddsStepErrors()
  {
    var supply = DefaultSupply with { VoltageStep = 0.5m, CurrentStep = 0.01m };
    var model = Parse("310 ВИ *3, 24.1В, 105мА*", supply);

    Assert.Contains(model.Errors, error => error.Code == ErrorCode.Vi_InvalidVoltage);
    Assert.Contains(model.Errors, error => error.Code == ErrorCode.Vi_InvalidCurrent);
  }

  [Fact]
  public void Parse_DuplicateConfiguredNumber_AddsAmbiguityError()
  {
    var parser = CreateParser(DefaultSupply, DefaultSupply with { NumberChassis = 2 });

    var model = Assert.IsType<ViCommandModel>(parser.Parse(
      "320", "ВИ", 1, new List<string> { "320 ВИ *3*" }));

    Assert.Contains(model.Errors, error => error.Code == ErrorCode.Vi_AmbiguousPowerSupply);
  }

  [Theory]
  [InlineData("310 ВИ *3, 24В*")]
  [InlineData("310 ВИ *3, 24В, 100мА, +A2*")]
  [InlineData("310 ВИ *3, 24В, 100мА, +A1, -B2*")]
  public void Parse_InvalidParameterCombination_AddsError(string source)
  {
    var model = Parse(source);

    Assert.NotEmpty(model.Errors);
  }

  [Theory]
  [InlineData("310 ВИ *3, 24В, 100мА, +A1, -B1*", "+A1,-B1")]
  public void Parse_PintUnsupportedBusPair_AddsBusError(string source, string expectedFragment)
  {
    var model = Parse(source);

    var error = Assert.Single(model.Errors, error => error.Code == ErrorCode.Vi_InvalidBus);
    Assert.Equal(expectedFragment, error.SourceFragment);
  }

  [Fact]
  public void Parse_FourthBusPair_BuildsConnectOperation()
  {
    var model = Parse("310 ВИ *3, 24В, 100мА, +A4, -B4*");

    Assert.Equal(ViOperation.ConfigureAndConnect, model.Operation);
    Assert.Empty(model.Errors);
  }

  [Fact]
  public void ResolveBusPairs_UsesRelayModulesOfSelectedTester()
  {
    var modules = new[]
    {
      new ModuleRelayControl { NumberChassis = 1, BusType = SwitchingBusNew.AB2 },
      new ModuleRelayControl { NumberChassis = 1, BusType = SwitchingBusNew.AB4 },
      new ModuleRelayControl { NumberChassis = 2, BusType = SwitchingBusNew.AB3 },
    };

    SupportedBusPairs pairs = RelayModuleBusPairResolver.Resolve(modules, 1);

    Assert.Equal(SupportedBusPairs.AB2 | SupportedBusPairs.AB4, pairs);
  }

  private static ViCommandModel Parse(string source, params ViPowerSupplyLimits[] supplies)
  {
    var parser = CreateParser(supplies.Length == 0 ? new[] { DefaultSupply } : supplies);
    string[] header = source.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
    return Assert.IsType<ViCommandModel>(parser.Parse(
      header[0], header[1], 1, new List<string> { source }));
  }

  private static ViCommandParser CreateParser(params ViPowerSupplyLimits[] supplies)
    => new(() => supplies);
}

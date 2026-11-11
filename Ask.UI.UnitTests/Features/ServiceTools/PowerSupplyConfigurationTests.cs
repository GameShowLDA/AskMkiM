using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.Device.Runtime.Device;
using System.IO;
using System.Text.Json;
using UI.Controls.Settings.Configuration;

namespace Ask.UI.UnitTests.Features.ServiceTools;

public sealed class PowerSupplyConfigurationTests
{
  [Fact]
  public void Configuration_RoundTripsSourceWithTesterIpAndPort()
  {
    var configuration = new DeviceConfigurationFileModel
    {
      Chassis = [new ChassisManagerDto { Number = 2, Name = "Тестер", DeviceClass = typeof(ManagerChassis).FullName! }],
      PowerSupplies = [new PowerSupplyDto { NumberChassis = 2, Number = 17, Name = "Б5-108",
        ConnectionDetails = "192.168.2.17", Port = 5080, DeviceClass = typeof(B5108).FullName!,
        MinVoltage = 1m, MaxVoltage = 120m, VoltageStep = 0.5m,
        MinCurrent = 0.01m, MaxCurrent = 2.01m, CurrentStep = 0.01m }],
    };
    var parsed = DeviceConfigurationService.ParseConfigurationFile(JsonSerializer.Serialize(configuration));
    DeviceConfigurationService.ValidateImportedConfiguration(parsed);
    var source = Assert.Single(parsed.PowerSupplies);
    Assert.Equal(2, source.NumberChassis);
    Assert.Equal(5080, source.Port);
    Assert.Equal("192.168.2.17", source.ConnectionDetails);
    Assert.Equal(1m, source.MinVoltage);
    Assert.Equal(120m, source.MaxVoltage);
    Assert.Equal(0.5m, source.VoltageStep);
    Assert.Equal(0.01m, source.MinCurrent);
    Assert.Equal(2.01m, source.MaxCurrent);
    Assert.Equal(0.01m, source.CurrentStep);
  }

  [Fact]
  public void Configuration_RejectsSourceLinkedToAnotherTester()
  {
    var configuration = new DeviceConfigurationFileModel
    {
      Chassis = [new ChassisManagerDto { Number = 1, DeviceClass = typeof(ManagerChassis).FullName! }],
      PowerSupplies = [new PowerSupplyDto { NumberChassis = 2, Number = 17, DeviceClass = typeof(B5108).FullName! }],
    };
    Assert.Throws<InvalidDataException>(() => DeviceConfigurationService.ValidateImportedConfiguration(configuration));
  }

  [Fact]
  public void LegacyConfiguration_WithoutPowerSupplySection_RemainsReadable()
  {
    var configuration = DeviceConfigurationService.ParseConfigurationFile("{\"Version\":2}");
    Assert.Empty(configuration.PowerSupplies);
  }

  [Fact]
  public void LegacySource_WithoutOperatingLimits_UsesB5108Defaults()
  {
    string json = $$"""
      {
        "Version": 2,
        "Chassis": [{ "Number": 2, "DeviceClass": "{{typeof(ManagerChassis).FullName}}" }],
        "PowerSupplies": [{
          "NumberChassis": 2,
          "Number": 17,
          "Port": 80,
          "DeviceClass": "{{typeof(B5108).FullName}}"
        }]
      }
      """;

    var configuration = DeviceConfigurationService.ParseConfigurationFile(json);
    DeviceConfigurationService.ValidateImportedConfiguration(configuration);
    var source = Assert.Single(configuration.PowerSupplies);
    Assert.Equal(0m, source.MinVoltage);
    Assert.Equal(150m, source.MaxVoltage);
    Assert.Equal(0.1m, source.VoltageStep);
    Assert.Equal(0m, source.MinCurrent);
    Assert.Equal(5m, source.MaxCurrent);
    Assert.Equal(0.001m, source.CurrentStep);
  }
}

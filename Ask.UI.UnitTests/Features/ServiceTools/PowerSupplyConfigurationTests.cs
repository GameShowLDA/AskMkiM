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
        ConnectionDetails = "192.168.2.17", Port = 5080, DeviceClass = typeof(B5108).FullName! }],
    };
    var parsed = DeviceConfigurationService.ParseConfigurationFile(JsonSerializer.Serialize(configuration));
    DeviceConfigurationService.ValidateImportedConfiguration(parsed);
    var source = Assert.Single(parsed.PowerSupplies);
    Assert.Equal(2, source.NumberChassis);
    Assert.Equal(5080, source.Port);
    Assert.Equal("192.168.2.17", source.ConnectionDetails);
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
}

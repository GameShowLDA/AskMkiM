using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.DataBase.Engine.Initialization;
using Ask.DataBase.Engine.Static.Devices;

namespace Ask.Engine.UnitTests.Fixtures;

public sealed class VshParserDbFixture : IDisposable
{
  private readonly int? createdChassisManagerId;

  public VshParserDbFixture()
  {
    DatabaseEngineInitializer.InitializeAsync().GetAwaiter().GetResult();

    if (ChassisManagers.GetByNumberAsync(1).GetAwaiter().GetResult() is not null)
    {
      return;
    }

    var chassisDto = new ChassisManagerDto
    {
      Name = "Unit test chassis manager",
      Description = "Temporary chassis manager for VSH parser tests",
      Number = 1,
      ConnectionDetails = "UNIT-TEST",
      DeviceType = DeviceType.ChassisManager,
      DeviceClass = "Ask.Device.Runtime.Device.ManagerChassis",
      BusType = BusStructureEnum.Type.Bus2
    };

    var chassis = ChassisManagers.Build(chassisDto);
    var created = ChassisManagers.CreateAsync(chassis).GetAwaiter().GetResult();
    createdChassisManagerId = created.Id;
  }

  public void Dispose()
  {
    if (!createdChassisManagerId.HasValue)
    {
      return;
    }

    var entity = ChassisManagers.GetByIdAsync(createdChassisManagerId.Value).GetAwaiter().GetResult();

    if (entity is not null)
    {
      ChassisManagers.DeleteAsync(entity).GetAwaiter().GetResult();
    }
  }
}

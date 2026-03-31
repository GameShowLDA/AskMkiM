using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.DataBase.Engine.Initialization;
using Ask.DataBase.Engine.Static.Devices;

namespace Ask.Engine.UnitTests.Fixtures;

public sealed class FastMeterDbFixture : IDisposable
{
  private readonly int? createdFastMeterId;

  public FastMeterDbFixture()
  {
    DatabaseEngineInitializer.InitializeAsync().GetAwaiter().GetResult();

    if (FastMeters.GetAllAsync().GetAwaiter().GetResult().Any())
    {
      return;
    }

    var fastMeterDto = new FastMeterDto
    {
      Name = "Unit test fast meter",
      Description = "Temporary fast meter for KS tests",
      NumberChassis = 9999,
      Number = 1,
      ConnectionDetails = "UNIT-TEST",
      DeviceClass = "Ask.Device.Runtime.Device.KeysightDevice",
      MaxContinuityResistance = 100
    };

    var device = FastMeters.Build(fastMeterDto);
    var created = FastMeters.CreateAsync(device).GetAwaiter().GetResult();
    createdFastMeterId = created.Id;
  }

  public void Dispose()
  {
    if (!createdFastMeterId.HasValue)
    {
      return;
    }

    var entity = FastMeters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault(item => item.Id == createdFastMeterId.Value);

    if (entity is not null)
    {
      FastMeters.DeleteAsync(entity).GetAwaiter().GetResult();
    }
  }
}

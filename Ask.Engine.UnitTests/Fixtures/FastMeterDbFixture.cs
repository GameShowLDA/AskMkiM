using Ask.Core.Shared.Entity.Devices;
using Ask.DataBase.Engine.Static.Devices;
using DataBaseConfiguration;

namespace Ask.Engine.UnitTests.Fixtures;

public sealed class FastMeterDbFixture : IDisposable
{
  private readonly int? createdFastMeterId;

  public FastMeterDbFixture()
  {
    DataBaseConfig.InitializeDB().GetAwaiter().GetResult();

    if (FastMeters.GetAllAsync().GetAwaiter().GetResult().Any())
    {
      return;
    }

    var entity = new FastMeterEntity
    {
      Name = "Unit test fast meter",
      Description = "Temporary fast meter for KS tests",
      NumberChassis = 9999,
      Number = 1,
      ConnectionDetails = "UNIT-TEST",
      DeviceClass = typeof(FastMeterEntity).AssemblyQualifiedName ?? typeof(FastMeterEntity).FullName ?? nameof(FastMeterEntity),
      MaxContinuityResistance = 100
    };

    FastMeters.CreateAsync(entity).GetAwaiter();
    createdFastMeterId = entity.Id;
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
      FastMeters.DeleteAsync(entity).GetAwaiter();
    }
  }
}

using Ask.Core.Shared.Entity.Devices;
using DataBaseConfiguration;
using DataBaseConfiguration.Services.Device;

namespace Ask.Engine.UnitTests.Fixtures;

public sealed class FastMeterDbFixture : IDisposable
{
  private readonly int? createdFastMeterId;

  public FastMeterDbFixture()
  {
    DataBaseConfig.InitializeDB().GetAwaiter().GetResult();

    var fastMeterServices = new FastMeterServices();
    fastMeterServices.ReloadCache();

    if (fastMeterServices.GetAll().Any())
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

    fastMeterServices.Create(entity);
    createdFastMeterId = entity.Id;
  }

  public void Dispose()
  {
    if (!createdFastMeterId.HasValue)
    {
      return;
    }

    var fastMeterServices = new FastMeterServices();
    var entity = fastMeterServices
      .GetAllEntities()
      .FirstOrDefault(item => item.Id == createdFastMeterId.Value);

    if (entity is not null)
    {
      fastMeterServices.Delete(entity);
    }
  }
}

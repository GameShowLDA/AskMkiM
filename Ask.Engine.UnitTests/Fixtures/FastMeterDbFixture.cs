using Ask.Core.Shared.DTO.Devices.FastMeter;
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

    var fastMeterDto = new FastMeterDto
    {
      Name = "Unit test fast meter",
      Description = "Temporary fast meter for KS tests",
      NumberChassis = 9999,
      Number = 1,
      ConnectionDetails = "UNIT-TEST",
      DeviceClass = typeof(FastMeterDto).AssemblyQualifiedName ?? typeof(FastMeterDto).FullName ?? nameof(FastMeterDto),
      MaxContinuityResistance = 100
    };

    var device = FastMeters.Build(fastMeterDto);
    FastMeters.CreateAsync(device).GetAwaiter();

    createdFastMeterId = fastMeterDto.Id;
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

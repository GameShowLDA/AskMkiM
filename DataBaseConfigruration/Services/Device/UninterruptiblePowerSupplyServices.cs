using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;

namespace DataBaseConfiguration.Services.Device
{
  /// <summary>
  /// Service for UPS entities.
  /// </summary>
  public class UninterruptiblePowerSupplyServices : Service<IUninterruptiblePowerSupply>
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="UninterruptiblePowerSupplyServices"/> class.
    /// </summary>
    public UninterruptiblePowerSupplyServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(IUninterruptiblePowerSupply entity)
    {
      bool exists = _context.Set<UninterruptiblePowerSupplyEntity>().Any(e => e.NumberChassis == entity.NumberChassis && e.Number == entity.Number);
      if (exists)
      {
        throw new DuplicateEntityException($"Бесперебойник с шасси {entity.NumberChassis} и адресом {entity.Number} уже существует.");
      }

      base.Create(entity);
    }

    /// <summary>
    /// Gets all UPS entities by chassis number.
    /// </summary>
    public List<UninterruptiblePowerSupplyEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return GetAllData()
        .OfType<UninterruptiblePowerSupplyEntity>()
        .Where(device => device.NumberChassis == numberChassis)
        .ToList();
    }

    /// <summary>
    /// Gets all UPS entities.
    /// </summary>
    public List<UninterruptiblePowerSupplyEntity> GetAllEntities()
    {
      return GetAllData().OfType<UninterruptiblePowerSupplyEntity>().ToList();
    }
  }
}

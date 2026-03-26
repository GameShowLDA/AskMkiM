using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с DTO быстрых измерителей.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class FastMeterDtoService : CrudService<FastMeterDto>
{
  /// <summary>
  /// Возвращает список быстрых измерителей, привязанных к указанному шасси.
  /// </summary>
  public Task<List<FastMeterDto>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    return GetByPredicateAsync(x => x.NumberChassis == numberChassis, cancellationToken);
  }
}

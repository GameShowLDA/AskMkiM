using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с DTO пробойных установок.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class BreakdownTesterDtoService : CrudService<BreakdownTesterDto>
{
  /// <summary>
  /// Возвращает список пробойных установок, привязанных к указанному шасси.
  /// </summary>
  public Task<List<BreakdownTesterDto>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    return GetByPredicateAsync(x => x.NumberChassis == numberChassis, cancellationToken);
  }
}

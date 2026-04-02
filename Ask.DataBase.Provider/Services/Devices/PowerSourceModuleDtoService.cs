using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с DTO модулей источника питания.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class PowerSourceModuleDtoService : CrudService<PowerSourceModuleDto>
{
  /// <summary>
  /// Возвращает список модулей источника питания, привязанных к указанному шасси.
  /// </summary>
  public Task<List<PowerSourceModuleDto>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    return GetByPredicateAsync(x => x.NumberChassis == numberChassis, cancellationToken);
  }
}

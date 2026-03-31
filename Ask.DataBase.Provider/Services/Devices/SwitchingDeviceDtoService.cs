using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с DTO устройств коммутации.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class SwitchingDeviceDtoService : CrudService<SwitchingDeviceDto>
{
  /// <summary>
  /// Возвращает список устройств коммутации, привязанных к указанному шасси.
  /// </summary>
  public Task<List<SwitchingDeviceDto>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    return GetByPredicateAsync(x => x.NumberChassis == numberChassis, cancellationToken);
  }
}

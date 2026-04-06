using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с DTO модулей коммутации реле.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class RelaySwitchModuleDtoService : CrudService<RelaySwitchModuleDto>
{
  /// <summary>
  /// Возвращает список модулей коммутации реле, привязанных к указанному шасси.
  /// </summary>
  public Task<List<RelaySwitchModuleDto>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    return GetByPredicateAsync(x => x.NumberChassis == numberChassis, cancellationToken);
  }

  /// <summary>
  /// Возвращает модуль коммутации реле по номеру шасси и номеру модуля.
  /// </summary>
  public Task<RelaySwitchModuleDto?> GetDeviceByNumberChassisAsync(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default)
  {
    return GetFirstOrDefaultAsync(
      x => x.NumberChassis == numberChassis && x.Number == number,
      cancellationToken);
  }

  /// <summary>
  /// Обновляет сопротивление модуля коммутации реле по номеру шасси и номеру модуля.
  /// </summary>
  public async Task UpdateResistanceAsync(
    int chassis,
    int module,
    double value,
    CancellationToken cancellationToken = default)
  {
    var entity = await GetDeviceByNumberChassisAsync(chassis, module, cancellationToken);
    if (entity == null)
    {
      throw new InvalidOperationException(
        $"Модуль коммутации реле с шасси {chassis} и номером {module} не найден.");
    }

    entity.SwitchResistance = value;
    await UpdateAsync(entity, cancellationToken);
  }
}

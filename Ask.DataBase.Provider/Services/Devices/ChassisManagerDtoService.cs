using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с DTO менеджеров шасси.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class ChassisManagerDtoService : CrudService<ChassisManagerDto>
{
  /// <summary>
  /// Возвращает менеджер шасси по номеру шасси.
  /// </summary>
  public Task<ChassisManagerDto?> GetByNumberAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    return GetFirstOrDefaultAsync(x => x.Number == numberChassis, cancellationToken);
  }  
}

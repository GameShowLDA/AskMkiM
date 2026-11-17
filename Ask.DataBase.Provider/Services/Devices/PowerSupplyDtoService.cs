using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сохраняет и загружает конфигурации внешних источников питания.
/// </summary>
public class PowerSupplyDtoService : CrudService<PowerSupplyDto>
{
  /// <summary>
  /// Возвращает источники, настроенные для выбранного тестера.
  /// </summary>
  /// <param name="numberChassis">Номер выбранного тестера.</param>
  /// <param name="cancellationToken">Токен отмены запроса.</param>
  /// <returns>Источники выбранного тестера.</returns>
  public Task<List<PowerSupplyDto>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default)
    => GetByPredicateAsync(device => device.NumberChassis == numberChassis, cancellationToken);

}

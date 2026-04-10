using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с DTO источников бесперебойного питания.
/// Дополнительные предметные методы пока не требуются.
/// </summary>
public class UninterruptiblePowerSupplyDtoService : CrudService<UninterruptiblePowerSupplyDto>
{
  /// <summary>
  /// Возвращает список UPS, привязанных к указанному шасси.
  /// </summary>
  public Task<List<UninterruptiblePowerSupplyDto>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    return GetByPredicateAsync(x => x.NumberChassis == numberChassis, cancellationToken);
  }
}

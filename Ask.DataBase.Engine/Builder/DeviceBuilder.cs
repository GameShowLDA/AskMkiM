using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.DataBase.Engine.Factory;
using Ask.DataBase.Engine.Mapping;

namespace Ask.DataBase.Engine.Builder;

/// <summary>
/// Построитель устройств.
/// Создаёт экземпляр устройства и применяет к нему данные из DTO,
/// используя фабрику и реестр мапперов.
/// </summary>
public static class DeviceBuilder
{
  /// <summary>
  /// Создаёт и инициализирует устройство на основе DTO.
  /// </summary>
  /// <param name="dto">DTO с данными устройства.</param>
  /// <returns>Готовый экземпляр устройства.</returns>
  public static IDevice Build(object dto)
  {
    if (dto == null)
      throw new ArgumentNullException(nameof(dto));

    dynamic d = dto;

    var device = DeviceFactory.Create(d.DeviceClass);

    DeviceMapperRegistry.Apply(device, dto);

    return device;
  }
}
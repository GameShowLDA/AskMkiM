using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.DataBase.Engine.Factory;
using Ask.DataBase.Engine.Mapping;
using Ask.Device.Application.Composition;

namespace Ask.DataBase.Engine.Builder;

/// <summary>
/// Построитель runtime-устройств.
/// Создаёт экземпляр устройства по <c>DeviceClass</c>, маппит DTO
/// и выполняет прикладную композицию менеджеров.
/// </summary>
public static class DeviceBuilder
{
  /// <summary>
  /// Создаёт runtime-устройство и возвращает его как <see cref="IDevice"/>.
  /// </summary>
  /// <param name="dto">DTO устройства.</param>
  /// <returns>Собранное runtime-устройство.</returns>
  public static IDevice Build(DeviceDto dto)
  {
    ArgumentNullException.ThrowIfNull(dto);

    var device = DeviceFactory.Create(dto.DeviceClass);
    DeviceMapperRegistry.Apply(device, dto);
    return DeviceApplicationComposer.Compose(device);
  }

  /// <summary>
  /// Создаёт runtime-устройство и возвращает его как нужный интерфейс устройства.
  /// </summary>
  /// <typeparam name="TDevice">Нужный интерфейс устройства.</typeparam>
  /// <param name="dto">DTO устройства.</param>
  /// <returns>Собранное runtime-устройство нужного интерфейса.</returns>
  public static TDevice Build<TDevice>(DeviceDto dto)
    where TDevice : class, IDevice
  {
    ArgumentNullException.ThrowIfNull(dto);

    var device = DeviceFactory.Create<TDevice>(dto.DeviceClass);
    DeviceMapperRegistry.Apply(device, dto);
    return DeviceApplicationComposer.Compose(device);
  }

  /// <summary>
  /// Создаёт runtime-устройство из объекта DTO.
  /// </summary>
  /// <param name="dto">DTO устройства.</param>
  /// <returns>Собранное runtime-устройство.</returns>
  public static IDevice Build(object dto)
  {
    if (dto is not DeviceDto deviceDto)
    {
      throw new ArgumentException("Ожидался DTO устройства.", nameof(dto));
    }

    return Build(deviceDto);
  }

  /// <summary>
  /// Создаёт runtime-устройство нужного интерфейса из объекта DTO.
  /// </summary>
  /// <typeparam name="TDevice">Нужный интерфейс устройства.</typeparam>
  /// <param name="dto">DTO устройства.</param>
  /// <returns>Собранное runtime-устройство нужного интерфейса.</returns>
  public static TDevice Build<TDevice>(object dto)
    where TDevice : class, IDevice
  {
    if (dto is not DeviceDto deviceDto)
    {
      throw new ArgumentException("Ожидался DTO устройства.", nameof(dto));
    }

    return Build<TDevice>(deviceDto);
  }
}

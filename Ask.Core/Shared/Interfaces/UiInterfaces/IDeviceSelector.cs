using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

/// <summary>
/// Предоставляет безопасный доступ к выбранным устройствам и параметрам интерфейса.
/// </summary>
public interface IDeviceSelector
{
  /// <summary>
  /// Возвращает выбранное релейное устройство.
  /// </summary>
  /// <returns>Экземпляр выбранного устройства или <see langword="null"/>.</returns>
  object? GetSelectedRelayDeviceByTypeSafe();

  /// <summary>
  /// Возвращает тип выбранного релейного устройства.
  /// </summary>
  /// <returns>Тип выбранного устройства.</returns>
  DeviceType GetSelectedRelayDeviceType();

  /// <summary>
  /// Возвращает выбранное значение режима самоконтроля.
  /// </summary>
  /// <returns>Значение перечисления или <see langword="null"/>, если выбор отсутствует.</returns>
  Enum? GetSelectedSelfControlEnumUntypedSafe();

  /// <summary>
  /// Возвращает выбранный мультиметр быстрого измерения.
  /// </summary>
  /// <returns>Экземпляр мультиметра или <see langword="null"/>, если он не выбран.</returns>
  IMultimeter? GetFastMeterSafe();
}
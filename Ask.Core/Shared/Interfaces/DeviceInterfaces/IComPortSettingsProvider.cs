using Ask.Core.Shared.DTO.Devices.Base;


/// <summary>
/// Предоставляет рекомендуемые параметры последовательного порта
/// для конкретной модели устройства, подключаемого по COM-интерфейсу.
/// </summary>
public interface IComPortSettingsProvider
{
  /// <summary>
  /// Возвращает параметры последовательного порта,
  /// рекомендуемые для данной модели устройства.
  /// Сохранённые пользовательские настройки могут переопределять эти значения.
  /// </summary>
  ComPortSettings DefaultComPortSettings { get; }
}
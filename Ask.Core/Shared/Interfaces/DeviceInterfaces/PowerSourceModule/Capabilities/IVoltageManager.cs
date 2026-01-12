using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities
{
  /// <summary>
  /// Интерфейс для управления напряжением модуля источника питания.
  /// </summary>
  public interface IVoltageManager
  {
    /// <summary>
    /// Устанавливает источник напряжения.
    /// </summary>
    /// <param name="voltageSources">Тип источника напряжения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task SetSourceVoltageAsync(VoltageSources voltageSources, IUserInteractionService? messageService = null);

    /// <summary>
    /// Устанавливает уровень напряжения.
    /// </summary>
    /// <param name="integerPart">Целая часть напряжения.</param>
    /// <param name="decimalPart">Дробная часть напряжения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task SetVoltageLevelAsync(int integerPart, int decimalPart, IUserInteractionService? messageService = null);
  }
}

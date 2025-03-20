using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.Function.ModuleVoltageCurrentSource
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
    Task SetSourceVoltageAsync(VoltageSources voltageSources);

    /// <summary>
    /// Устанавливает уровень напряжения.
    /// </summary>
    /// <param name="integerPart">Целая часть напряжения.</param>
    /// <param name="decimalPart">Дробная часть напряжения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task SetVoltageLevelAsync(int integerPart, int decimalPart);
  }
}

using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.DTO.Devices.Base
{
  /// <summary>
  /// Модель данных элемента.
  /// </summary>
  public class DataModel
  {
    /// <summary>
    /// Модель первой точки.
    /// </summary>
    public PointModel FirstPoint { get; set; }

    /// <summary>
    /// Модель второй точки.
    /// </summary>
    public PointModel SecondPoint { get; set; }

    /// <summary>
    /// Значение электрического параметра.
    /// </summary>
    public double Param { get; set; }

    /// <summary>
    /// Значение времени при выполнения теста (ППУ).
    /// </summary>
    public double Time { get; set; }

    /// <summary>
    /// Значение нарастания времени при выполнения теста (ППУ).
    /// </summary>
    public double RampTime { get; set; }

    /// <summary>
    /// Значение напряжения при выполнения теста (ППУ).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Заданная шина.
    /// </summary>
    public BusPoint ActiveBus { get; set; }

    /// <summary>
    /// Заданная пара шин.
    /// </summary>
    public SwitchingBusNew ActivePairBus { get; set; }

    /// <summary>
    /// Получает или задаёт номер проверяемого устройства в формате a.b.
    /// </summary>
    public string TestedNumber { get; set; }

    /// <summary>
    /// Получает или задаёт номер проверяющего устройства в формате a.b.
    /// </summary>
    public string TesterNumber { get; set; }

    /// <summary>
    /// Получает или задаёт диапазон проверки в формате списка чисел и диапазонов (например, "1-3,5").
    /// </summary>
    public string TestRange { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataModel"/>.
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    /// <param name="param">Значение электрического параметра.</param>
    public DataModel(PointModel first, PointModel second, double param)
    {
      FirstPoint = first;
      SecondPoint = second;
      Param = param;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataModel"/>.
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    /// <param name="param">Значение электрического параметра.</param>
    public DataModel() { }
  }
}

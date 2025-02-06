namespace NewCore.Enum
{
  public class DeviceEnum
  {
    /// <summary>
    /// Перечисление типов устройств
    /// </summary>
    public enum DeviceType
    {
      /// <summary>
      /// Менеджер шасси
      /// </summary>
      ChassisManager,

      /// <summary>
      /// Модуль коммутации реле
      /// </summary>
      RelaySwitchModule,

      /// <summary>
      /// Модуль источника напряжения и тока
      /// </summary>
      PowerSourceModule,

      /// <summary>
      /// Устройство коммутации
      /// </summary>
      SwitchingDevice,

      /// <summary>
      /// Измеритель точный
      /// </summary>
      PrecisionMeter,

      /// <summary>
      /// Измеритель быстрый
      /// </summary>
      FastMeter,

      /// <summary>
      /// Пробойная установка
      /// </summary>
      BreakdownTester
    }

    /// <summary>
    /// Тип подключения.
    /// </summary>
    public enum ConnectionType
    {
      IP,
      COM
    }

    /// <summary>
    /// Коммутационные шины системы.
    /// </summary>
    public enum SwitchingBus
    {
      /// <summary>
      /// Шина А1.
      /// </summary>
      A1,

      /// <summary>
      /// Шина А2.
      /// </summary>
      A2,

      /// <summary>
      /// Шина А3.
      /// </summary>
      A3,

      /// <summary>
      /// Шина А4.
      /// </summary>
      A4,

      /// <summary>
      /// Шина В1.
      /// </summary>
      B1,

      /// <summary>
      /// Шина В2.
      /// </summary>
      B2,

      /// <summary>
      /// Шина В3.
      /// </summary>
      B3,

      /// <summary>
      /// Шина В4.
      /// </summary>
      B4,

      /// <summary>
      /// Шина А1 и В1.
      /// </summary>
      AB1,

      /// <summary>
      /// Шина А2 и В2.
      /// </summary>
      AB2,

      /// <summary>
      /// Шина А3 и В3.
      /// </summary>
      AB3,

      /// <summary>
      /// Шина А4 и В4.
      /// </summary>
      AB4,
    }

    /// <summary>
    /// Словарь соответствий шин и их параметров (группа, номер).
    /// </summary>
    internal static readonly Dictionary<SwitchingBus, Tuple<int, int>> BusParameters = new Dictionary<SwitchingBus, Tuple<int, int>>
    {
      { SwitchingBus.A1, Tuple.Create(1, 1) },
      { SwitchingBus.A2, Tuple.Create(1, 2) },
      { SwitchingBus.A3, Tuple.Create(1, 3) },
      { SwitchingBus.A4, Tuple.Create(1, 4) },
      { SwitchingBus.B1, Tuple.Create(2, 1) },
      { SwitchingBus.B2, Tuple.Create(2, 2) },
      { SwitchingBus.B3, Tuple.Create(2, 3) },
      { SwitchingBus.B4, Tuple.Create(2, 4) },
      { SwitchingBus.AB1, Tuple.Create(3, 1) },
      { SwitchingBus.AB2, Tuple.Create(3, 2) },
      { SwitchingBus.AB3, Tuple.Create(3, 3) },
      { SwitchingBus.AB4, Tuple.Create(3, 4) },
    };

    /// <summary>
    /// Шины точек модуля коммутации реле.
    /// </summary>
    public enum BusPoint
    {
      /// <summary>
      /// Шина А.
      /// </summary>
      A = 1,

      /// <summary>
      /// Шина В.
      /// </summary>
      B = 2,
    }

    /// <summary>
    /// Источники напряжения.
    /// </summary>
    public enum VoltageSources
    {
      Supply12V,
      Supply5V,
    }

    /// <summary>
    /// Перечисление разъёмов измерителя.
    /// </summary>
    public enum MeterConnector
    {
      /// <summary>
      /// Разъём XS_3 на УКШ.
      /// </summary>
      XS3,

      /// <summary>
      /// Разъём XS_4 на УКШ.
      /// </summary>
      XS4,

      /// <summary>
      /// Разъём XS_5 на УКШ.
      /// </summary>
      XS5,
    }
  }
}

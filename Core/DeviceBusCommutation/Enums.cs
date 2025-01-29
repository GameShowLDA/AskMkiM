namespace Core.DeviceBusCommutation
{
  /// <summary>
  /// Перечисления различных данных "Устройства коммутации шин".
  /// </summary>
  static public class Enums
  {
    /// <summary>
    /// Шины устройства коммутации шин.
    /// </summary>
    public enum BusDeviceBusCommutation
    {
      /// <summary>
      /// Шина А1 УКШ.
      /// </summary>
      A1,

      /// <summary>
      /// Шина А2 УКШ.
      /// </summary>
      A2,

      /// <summary>
      /// Шина А3 УКШ.
      /// </summary>
      A3,

      /// <summary>
      /// Шина А4 УКШ.
      /// </summary>
      A4,

      /// <summary>
      /// Шина В1 УКШ.
      /// </summary>
      B1,

      /// <summary>
      /// Шина В2 УКШ.
      /// </summary>
      B2,

      /// <summary>
      /// Шина В3 УКШ.
      /// </summary>
      B3,

      /// <summary>
      /// Шина В4 УКШ.
      /// </summary>
      B4,

      /// <summary>
      /// Шина А1 и В1 УКШ.
      /// </summary>
      AB1,

      /// <summary>
      /// Шина А2 и В2 УКШ.
      /// </summary>
      AB2,

      /// <summary>
      /// Шина А3 и В3 УКШ.
      /// </summary>
      AB3,

      /// <summary>
      /// Шина А4 и В4 УКШ.
      /// </summary>
      AB4,
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

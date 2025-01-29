namespace Core.ModuleRelayControl
{
  /// <summary>
  /// Перечисления различных данных "Модуля коммутации реле".
  /// </summary>
  static public class Enums
  {
    /// <summary>
    /// Шины Модуля коммутации реле.
    /// </summary>
    public enum BusModuleRelayControl
    {
      /// <summary>
      /// Шина А1 МКР.
      /// </summary>
      A1,

      /// <summary>
      /// Шина А2 МКР.
      /// </summary>
      A2,

      /// <summary>
      /// Шина А3 МКР.
      /// </summary>
      A3,

      /// <summary>
      /// Шина А4 МКР.
      /// </summary>
      A4,

      /// <summary>
      /// Шина В1 МКР.
      /// </summary>
      B1,

      /// <summary>
      /// Шина В2 МКР.
      /// </summary>
      B2,

      /// <summary>
      /// Шина В3 МКР.
      /// </summary>
      B3,

      /// <summary>
      /// Шина В4 МКР.
      /// </summary>
      B4,

      /// <summary>
      /// Шина А1 и В1 МКР.
      /// </summary>
      AB1,

      /// <summary>
      /// Шина А2 и В2 МКР.
      /// </summary>
      AB2,

      /// <summary>
      /// Шина А3 и В3 МКР.
      /// </summary>
      AB3,

      /// <summary>
      /// Шина А4 и В4 МКР.
      /// </summary>
      AB4,
    }

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
  }
}

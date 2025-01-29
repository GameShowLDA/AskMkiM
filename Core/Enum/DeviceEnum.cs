namespace Core.Enum
{
  /// <summary>
  /// Перечисления связанные с устройствами.
  /// </summary>
  public class DeviceEnum
  {
    /// <summary>
    /// Тип оборудования.
    /// </summary>
    public enum Type
    {
      /// <summary>
      /// Укш.
      /// </summary>
      DeviceBusCommutation,

      /// <summary>
      /// Модуль коммутации реле.
      /// </summary>
      ModuleRelayControl,

      /// <summary>
      /// Менеджер шасси.
      /// </summary>
      ManagerShassy,

      /// <summary>
      /// Модуль источника напряжения и тока.
      /// </summary>
      ModuleVoltageCurrentSource,

      /// <summary>
      /// Точный измеритель.
      /// </summary>
      AccurateMeter,

      /// <summary>
      /// Быстрый измеритель.
      /// </summary>
      FastMeter,

      /// <summary>
      /// Пробойная установка.
      /// </summary>
      Breakdown,
    }

    /// <summary>
    /// Тип напряжения.
    /// </summary>
    public enum VoltageType
    {
      /// <summary>
      /// Высоковольтный.
      /// </summary>
      HightVoltage,

      /// <summary>
      /// Низковольтный.
      /// </summary>
      LowVoltage,
    }

    /// <summary>
    /// Блоки для самоконтроля УКШ.
    /// </summary>
    public enum RelayCheck
    {
      /// <summary>
      /// Проверка земляных реле у мультиметра.
      /// </summary>
      CheckEarthRelayMultimeter = 1,

      /// <summary>
      /// Проверка реле с переполюсовкой у мультиметра.
      /// </summary>
      CheckPolarityReversalRelayMultimeter = 2,

      /// <summary>
      /// Проверка реле выходящих на шины АЦП.
      /// </summary>
      CheckADCBusRelays = 3,

      /// <summary>
      /// Проверка земляных реле и реле с переполюсовкой в блоке АЦП.
      /// </summary>
      CheckADCRelaysAndPolarityReversal = 4,

      /// <summary>
      /// Проверка реле выходящих на шины в блоке ПИНТ.
      /// </summary>
      CheckRelayOutputsInPINT = 5,

      /// <summary>
      /// Проверка реле переполюсовки в блоке ПИНТ.
      /// </summary>
      CheckPolarityReversalInPINT = 6,

      /// <summary>
      /// Проверка реле шунта.
      /// </summary>
      CheckShuntRelay = 7,

      /// <summary>
      /// Проверка реле шины А1.
      /// </summary>
      CheckBusA1Relay = 8,

      /// <summary>
      /// Проверка реле шины В1".
      /// </summary>
      CheckBusB1Relay = 9,
    }

    /// <summary>
    /// возвращает описание устройств.
    /// </summary>
    /// <param name="value">Тип устройства.</param>
    /// <returns>Объект Chain для указанного коннектора.</returns>
    public static Tuple<string, string> GetInfoDevice(Type value)
    {
      Tuple<string, string> returnData;
      infoDevice.TryGetValue(value, out returnData);
      return returnData;
    }

    /// <summary>
    /// Возвращение наименования блоков.
    /// </summary>
    /// <param name="checkType">Тип проверяемого блока.</param>
    /// <returns></returns>
    public static string GetInfoBlock(RelayCheck checkType)
    {
      string info;
      infoBlock.TryGetValue(checkType, out info);
      return info;
    }

    /// <summary>
    /// Возвращение названия блока цепей.
    /// </summary>
    /// <param name="checkType">Тип блока цепей.</param>
    /// <returns>Строковое название блока цепи.</returns>
    public static Tuple<int, List<List<int>>> GetSelfTestDeviceBusCommutation(RelayCheck checkType)
    {
      Tuple<int, List<List<int>>> number;
      relayCheckMappings.TryGetValue(checkType, out number);
      return number;
    }


    private static Dictionary<RelayCheck, Tuple<int, List<List<int>>>> relayCheckMappings = new Dictionary<RelayCheck, Tuple<int, List<List<int>>>>
    {
      {
        RelayCheck.CheckEarthRelayMultimeter, Tuple.Create(1, new List<List<int>>
        {
          new List<int> { 4, 3 },
        })
      },

      // Проверка реле с переполюсовкой у мультиметра
      {
        RelayCheck.CheckPolarityReversalRelayMultimeter, Tuple.Create(4, new List<List<int>>
        {
            new List<int> { 111, 123 },
            new List<int> { 112, 124 },
            new List<int> { 125, 113 },
            new List<int> { 126, 114 },
        })
      },

      // Проверка реле выходящих на шины АЦП
      {
        RelayCheck.CheckADCBusRelays, Tuple.Create(8, new List<List<int>>
          {
              new List<int> { 111, 41, 25, 120, 109, 33, 49, 113 },
              new List<int> { 111, 42, 26, 120, 109, 34, 50, 113 },
              new List<int> { 111, 43, 27, 120, 109, 35, 51, 113 },
              new List<int> { 111, 44, 28, 120, 109, 36, 52, 113 },
              new List<int> { 111, 45, 29, 120, 109, 37, 53, 113 },
              new List<int> { 111, 46, 30, 120, 109, 38, 54, 113 },
              new List<int> { 111, 47, 31, 120, 109, 39, 55, 113 },
              new List<int> { 111, 48, 32, 120, 109, 40, 56, 113 },
          })
      },

      // Проверка земляных реле и реле с переполюсовкой в блоке АЦП
      {
        RelayCheck.CheckADCRelaysAndPolarityReversal, Tuple.Create(8, new List<List<int>>
          {
              new List<int> { 3, 2, 107, 25, 41, 111 },
              new List<int> { 3, 2, 108, 25, 41, 111 },
              new List<int> { 3, 2, 121, 33, 49, 125 },
              new List<int> { 3, 2, 122, 33, 49, 125 },
              new List<int> { 4, 1, 119, 25, 41, 123 },
              new List<int> { 4, 1, 120, 25, 41, 123 },
              new List<int> { 4, 1, 109, 33, 49, 113 },
              new List<int> { 4, 1, 110, 33, 49, 113 },
          })
      },

      // Проверка реле выходящих на шины в блоке ПИНТ
      {
        RelayCheck.CheckRelayOutputsInPINT, Tuple.Create(8, new List<List<int>>
          {
              new List<int> { 111, 41, 57, 128, 117, 65, 49, 113 },
              new List<int> { 111, 42, 58, 128, 117, 66, 50, 113 },
              new List<int> { 111, 43, 59, 128, 117, 67, 51, 113 },
              new List<int> { 111, 44, 60, 128, 117, 68, 52, 113 },
              new List<int> { 111, 45, 61, 128, 117, 69, 53, 113 },
              new List<int> { 111, 46, 62, 128, 117, 70, 54, 113 },
              new List<int> { 111, 47, 63, 128, 117, 71, 55, 113 },
              new List<int> { 111, 48, 64, 128, 117, 72, 56, 113 },
          })
      },

      // Проверка реле переполюсовки в блоке ПИНТ
      {
        RelayCheck.CheckPolarityReversalInPINT, Tuple.Create(3, new List<List<int>>
          {
              new List<int> { 111, 41, 57, 115, 129, 65, 49, 113 },
              new List<int> { 111, 41, 57, 116, 130, 65, 49, 113 },
              new List<int> { 111, 41, 57, 127, 118, 65, 49, 113 },
          })
      },

      // Проверка реле шунта
      {
        RelayCheck.CheckShuntRelay, Tuple.Create(3, new List<List<int>>
          {
              new List<int> { 111, 41, 57, 115, 106, 16 },
              new List<int> { 111, 41, 57, 115, 105, 16 },
              new List<int> { 111, 41, 57, 115, 15 },
          })
      },

      // Проверка реле шины А1
      {
        RelayCheck.CheckBusA1Relay, Tuple.Create(2, new List<List<int>>
          {
              new List<int> { 111, 41, 89, 7, 3 },
              new List<int> { 111, 41, 90, 7, 3 },
          })
      },

      // Проверка реле шины В1
      {
        RelayCheck.CheckBusB1Relay, Tuple.Create(2, new List<List<int>>
          {
              new List<int> { 113, 49, 97, 11, 4 },
              new List<int> { 113, 49, 98, 11, 4 },
          })
      },
    };

    /// <summary>
    /// Словарь информации об устройствах
    /// </summary>
    static private Dictionary<Type, Tuple<string, string>> infoDevice = new System.Collections.Generic.Dictionary<Type, Tuple<string, string>>()
    {
      [Type.DeviceBusCommutation] = Tuple.Create("УКШ", "Реализовать описание в DeviceEnum"),
      [Type.ModuleRelayControl] = Tuple.Create("МКР", " предназначен для коммутации измерительных шин автоматизированной системы контроля к высоковольтным цепям объектов контроля, таких как кабели, жгуты, кабельные сети. Коммутация происходит за счет замыкания реле на шину"),
      [Type.ManagerShassy] = Tuple.Create("Менеджер шасси", "предназначен для управления питанием модулей, управления системой охлаждения, для активации модулей, дежурной активации шасси при включенном питании, дезактивации модулей, отключении шасси при завершении работы"),
      [Type.ModuleVoltageCurrentSource] = Tuple.Create("МИНТ", "Модуль  предназначен для создания электрических параметров для проверки кабельных изделий, печатных плат, контроля функционирования релейно-коммутационных изделий и другой подобной аппаратуры, проведения испытаний изделий по программам контроля"),
      [Type.AccurateMeter] = Tuple.Create("Точный измеритель", "Реализовать описание в DeviceEnum"),
      [Type.FastMeter] = Tuple.Create("Быстрый измеритель", "Реализовать описание в DeviceEnum"),
      [Type.Breakdown] = Tuple.Create("Пробойная установка", "Реализовать описание в DeviceEnum"),
    };

    /// <summary>
    /// Названия блоков самоконтроля УКШ.
    /// </summary>
    static private Dictionary<RelayCheck, string> infoBlock = new Dictionary<RelayCheck, string>
    {
        { RelayCheck.CheckEarthRelayMultimeter, "Проверка земляных реле у мультиметра" },
        { RelayCheck.CheckPolarityReversalRelayMultimeter, "Проверка реле с переполюсовкой у мультиметра" },
        { RelayCheck.CheckADCBusRelays, "Проверка реле выходящих на шины АЦП" },
        { RelayCheck.CheckADCRelaysAndPolarityReversal,"Проверка земляных реле и реле с переполюсовкой в блоке АЦП" },
        { RelayCheck.CheckRelayOutputsInPINT, "Проверка реле выходящих на шины в блоке ПИНТ" },
        { RelayCheck.CheckPolarityReversalInPINT, "Проверка реле переполюсовки в блоке ПИНТ" },
        { RelayCheck.CheckShuntRelay, "Проверка реле шунта" },
        { RelayCheck.CheckBusA1Relay, "Проверка реле шины А1" },
        { RelayCheck.CheckBusB1Relay, "Проверка реле шины В1" },
    };
  }
}

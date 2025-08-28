using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Function.DBC;
using NewCore.Communication;
using NewCore.Device;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.LoggerUtility;


namespace NewCore.Function.DeviceBusCommutation.SelfCheck
{
  internal static class SelfTestMetadataProvider
  {

    /// <summary>
    /// Словарь допустимых комбинаций шины и контактов для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<TypeConnector, List<int>> ValidBusContacts = new()
        {
            { TypeConnector.BlockingRelay, new List<int> { 11, 21 } },
            { TypeConnector.Multimeter, new List<int> { 11,12,13,14,21,22,23,24} },
            { TypeConnector.ADC, new List<int> { 11,12,13,14,21,22,23,24} },
            { TypeConnector.ADCReversed, new List<int> { 11,12,13,14,21,22,23,24} },
            { TypeConnector.PINT, new List<int> { 12, 13, 22, 23 } },
            { TypeConnector.Shunt, new List<int> { 1, 2 } },
            { TypeConnector.BreakdownTester, new List<int> { 11, 21 } },
        };

    /// <summary>
    /// Словарь, содержащий названия цепей для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<TypeConnector, string> CircuitNames = new()
        {
            { TypeConnector.BlockingRelay, "Блокировочное реле" },
            { TypeConnector.Multimeter, "Мультиметр" },
            { TypeConnector.ADC, "АЦП" },
            { TypeConnector.ADCReversed, "АЦП с переполюсовкой" },
            { TypeConnector.PINT, "ПИНТ" },
            { TypeConnector.Shunt, "Шунт" },
            { TypeConnector.BreakdownTester, "ППУ" },
        };

    /// <inheritdoc />
    static public List<int>? GetValidBusContacts(TypeConnector testType)
    {
      return ValidBusContacts.TryGetValue(testType, out var contacts) ? contacts : null;
    }

    /// <inheritdoc />
    static public string GetCircuitName(TypeConnector testType, int busContact)
    {
      if (CircuitNames.TryGetValue(testType, out string? circuitName))
      {
        return $"{circuitName}, контакт {busContact}";
      }

      return $"Неизвестная цепь, контакт {busContact}";
    }

    /// <inheritdoc />
    static public async Task<int> GetRelayCountAsync(Device.DeviceBusCommutation _deviceBusCommutation, TypeConnector testType, int busContact)
    {
      if (await GetIsIdleModeEnabled())
      {
        return 0;
      }

      DeviceCommand cmd = new DeviceCommand(41, (int)testType * 10, busContact, 0);
      string response = await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

      if (int.TryParse(response, out int relayCount))
      {
        LogInformation($"Количество реле в цепи {testType}: {relayCount}", isDeviceLog: true);
        return relayCount;
      }

      LogError($"Ошибка получения количества реле для {testType}", isDeviceLog: true);
      return -1;
    }
    
    static public IEnumerable<object> GetSupportedTestTypes()
    {
      return SelfTestMetadataProvider.ValidBusContacts.Keys.Cast<object>();
    }

    static public Type GetTestTypeEnum()
    {
      return typeof(TypeConnector);
    }
  }
}

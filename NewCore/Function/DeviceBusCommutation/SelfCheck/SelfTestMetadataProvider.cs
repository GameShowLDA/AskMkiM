using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Udp;
using static Ask.LogLib.LoggerUtility;


namespace NewCore.Function.DeviceBusCommutation.SelfCheck
{
  internal static class SelfTestMetadataProvider
  {

    /// <summary>
    /// Словарь допустимых комбинаций шины и контактов для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<SwitchingDeviceTypeConnector, List<int>> ValidBusContacts = new()
        {
            { SwitchingDeviceTypeConnector.BlockingRelay, new List<int> { 11, 21 } },
            { SwitchingDeviceTypeConnector.Multimeter, new List<int> { 11,12,13,14,21,22,23,24} },
            { SwitchingDeviceTypeConnector.ADC, new List<int> { 11,12,13,14,21,22,23,24} },
            { SwitchingDeviceTypeConnector.ADCReversed, new List<int> { 11,12,13,14,21,22,23,24} },
            { SwitchingDeviceTypeConnector.PINT, new List<int> { 12, 13, 22, 23 } },
            { SwitchingDeviceTypeConnector.Shunt, new List<int> { 1, 2 } },
            { SwitchingDeviceTypeConnector.BreakdownTester, new List<int> { 11, 21 } },
        };

    /// <summary>
    /// Словарь, содержащий названия цепей для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<SwitchingDeviceTypeConnector, string> CircuitNames = new()
        {
            { SwitchingDeviceTypeConnector.BlockingRelay, "Блокировочное реле" },
            { SwitchingDeviceTypeConnector.Multimeter, "Мультиметр" },
            { SwitchingDeviceTypeConnector.ADC, "АЦП" },
            { SwitchingDeviceTypeConnector.ADCReversed, "АЦП с переполюсовкой" },
            { SwitchingDeviceTypeConnector.PINT, "ПИНТ" },
            { SwitchingDeviceTypeConnector.Shunt, "Шунт" },
            { SwitchingDeviceTypeConnector.BreakdownTester, "ППУ" },
        };

    /// <inheritdoc />
    static public List<int>? GetValidBusContacts(SwitchingDeviceTypeConnector testType)
    {
      return ValidBusContacts.TryGetValue(testType, out var contacts) ? contacts : null;
    }

    /// <inheritdoc />
    static public string GetCircuitName(SwitchingDeviceTypeConnector testType, int busContact)
    {
      if (CircuitNames.TryGetValue(testType, out string? circuitName))
      {
        return $"{circuitName}, контакт {busContact}";
      }

      return $"Неизвестная цепь, контакт {busContact}";
    }

    /// <inheritdoc />
    static public async Task<int> GetRelayCountAsync(Device.DeviceBusCommutation _deviceBusCommutation, SwitchingDeviceTypeConnector testType, int busContact)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
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
      return typeof(SwitchingDeviceTypeConnector);
    }
  }
}

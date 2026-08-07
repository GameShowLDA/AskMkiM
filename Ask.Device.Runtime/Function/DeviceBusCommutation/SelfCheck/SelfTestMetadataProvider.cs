using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Commands;
using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation.SelfCheck
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
            //{ SwitchingDeviceTypeConnector.ADC, new List<int> { 11,12,13,14,21,22,23,24} },
            //{ SwitchingDeviceTypeConnector.ADCReversed, new List<int> { 11,12,13,14,21,22,23,24} },
            //{ SwitchingDeviceTypeConnector.PINT, new List<int> { 12, 13, 22, 23 } },
            //{ SwitchingDeviceTypeConnector.Shunt, new List<int> { 1, 2 } },
            { SwitchingDeviceTypeConnector.BreakdownTester, new List<int> { 11, 21 } },
        };

    /// <summary>
    /// Словарь, содержащий названия цепей для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<SwitchingDeviceTypeConnector, string> CircuitNames = new()
        {
            { SwitchingDeviceTypeConnector.BlockingRelay, "Блокировочное реле" },
            { SwitchingDeviceTypeConnector.Multimeter, "Мультиметр" },
            //{ SwitchingDeviceTypeConnector.ADC, "АЦП" },
            //{ SwitchingDeviceTypeConnector.ADCReversed, "АЦП с переполюсовкой" },
            //{ SwitchingDeviceTypeConnector.PINT, "ПИНТ" },
            //{ SwitchingDeviceTypeConnector.Shunt, "Шунт" },
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
      var bus = GetBusContactName(busContact);
      if (CircuitNames.TryGetValue(testType, out string? circuitName))
      {
        return $"{circuitName}, шина {bus}";
      }

      return $"Неизвестная цепь, шина {bus}";
    }

    /// <inheritdoc />
    static public async Task<int> GetRelayCountAsync(Device.DeviceBusCommutation _deviceBusCommutation, SwitchingDeviceTypeConnector testType, int busContact)
    {
      DeviceCommand cmd = new DeviceCommand(41, (int)testType * 10, busContact, 0);
      string response = await new DeviceBusCommutationQueryExecutor(_deviceBusCommutation)
        .QueryAsync(cmd.ToString(), timeout: 2000);

      if (DeviceBusCommutationResponseProcessor.TryReadNumericResponse(response, out int relayCount))
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

    /// <summary>
    /// Преобразует номер шины и контакта в строковое представление.
    /// <para>
    /// Если передана одна цифра:
    /// <list type="bullet">
    /// <item><description>1 → A</description></item>
    /// <item><description>2 → B</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Если переданы две цифры:
    /// <list type="bullet">
    /// <item><description>11 → A1</description></item>
    /// <item><description>12 → A2</description></item>
    /// <item><description>13 → A3</description></item>
    /// <item><description>14 → A4</description></item>
    /// <item><description>21 → B1</description></item>
    /// <item><description>22 → B2</description></item>
    /// <item><description>23 → B3</description></item>
    /// <item><description>24 → B4</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="busContact">Номер шины и контакта.</param>
    /// <returns>Строковое представление шины и контакта.</returns>
    public static string GetBusContactName(int busContact)
    {
      if (busContact is 1 or 2)
      {
        return busContact == 1 ? "A" : "B";
      }

      int bus = busContact / 10;
      int contact = busContact % 10;

      if ((bus != 1 && bus != 2) || contact is < 1 or > 4)
      {
        throw new ArgumentOutOfRangeException(
            nameof(busContact),
            $"Недопустимое значение: {busContact}");
      }

      return $"{(bus == 1 ? "A" : "B")}{contact}";
    }
  }
}

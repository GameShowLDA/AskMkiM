using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerDeviceBusCommutation
  {
    internal static bool MeterConnect = false;
    internal static bool DbcConnect = false;

    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public SelfTestManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <inheritdoc />
    public async Task<bool> ExecuteSelfTestAsync(CancellationToken cancellationToken, SwitchingDeviceTypeConnector testType, int busContact, int action, IUserInteractionService? userMessageService = null) => await SelfTestProcessManager.ExecuteSelfTestAsync(cancellationToken, _deviceBusCommutation, testType, busContact, action);
    /// <summary>
    /// Проверяет корректность переданных параметров.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие.</param>
    /// <returns><c>true</c>, если параметры корректны, иначе <c>false</c>.</returns>
    static internal bool ValidateParameters(SwitchingDeviceTypeConnector testType, int busContact, int action)
    {
      if (!SelfTestMetadataProvider.ValidBusContacts.ContainsKey(testType) || action < 1 || action > 2)
      {
        return false;
      }

      return SelfTestMetadataProvider.ValidBusContacts[testType].Contains(busContact);
    }

    #region Полученние данных.

    /// <inheritdoc />
    public List<int>? GetValidBusContacts(SwitchingDeviceTypeConnector testType, IUserInteractionService? userMessageService = null) => SelfTestMetadataProvider.GetValidBusContacts(testType);

    /// <inheritdoc />
    public string GetCircuitName(SwitchingDeviceTypeConnector testType, int busContact, IUserInteractionService? userMessageService = null) => SelfTestMetadataProvider.GetCircuitName(testType, busContact);

    /// <inheritdoc />
    public async Task<int> GetRelayCountAsync(SwitchingDeviceTypeConnector testType, int busContact, IUserInteractionService? userMessageService = null) => await SelfTestMetadataProvider.GetRelayCountAsync(_deviceBusCommutation, testType, busContact);

    /// <inheritdoc />
    public IEnumerable<object> GetSupportedTestTypes() => SelfTestMetadataProvider.GetSupportedTestTypes();

    /// <inheritdoc />
    public Type GetTestTypeEnum() => SelfTestMetadataProvider.GetTestTypeEnum();

    #endregion

    /// <inheritdoc />
    public Task StartSelfCheck(CancellationToken cancellationToken, System.Enum selectedType, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IFastMeter meter = null) => SelfTestProcessManager.StartSelfCheck(cancellationToken, userMessageService, selectedType, device, meter);

    /// <inheritdoc />
    public async Task<bool> ControlRelayAsync(CancellationToken cancellationToken, SwitchingDeviceTypeConnector testType, int relayNumber, int busContact, int action, IUserInteractionService? userMessageService = null) => await SelfTestProcessManager.ControlRelayAsync(cancellationToken, _deviceBusCommutation, testType, relayNumber, busContact, action);


  }
}

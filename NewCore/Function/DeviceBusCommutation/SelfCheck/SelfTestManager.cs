using System.Net;
using AppConfiguration.Interface;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using Utilities.Models;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation.SelfCheck
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
    public async Task<bool> ExecuteSelfTestAsync(CancellationToken cancellationToken, TypeConnector testType, int busContact, int action) => await SelfTestProcessManager.ExecuteSelfTestAsync(cancellationToken, _deviceBusCommutation, testType, busContact, action);
    /// <summary>
    /// Проверяет корректность переданных параметров.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие.</param>
    /// <returns><c>true</c>, если параметры корректны, иначе <c>false</c>.</returns>
    static internal bool ValidateParameters(TypeConnector testType, int busContact, int action)
    {
      if (!SelfTestMetadataProvider.ValidBusContacts.ContainsKey(testType) || action < 1 || action > 2)
      {
        return false;
      }

      return SelfTestMetadataProvider.ValidBusContacts[testType].Contains(busContact);
    }

    #region Полученние данных.

    /// <inheritdoc />
    public List<int>? GetValidBusContacts(TypeConnector testType) => SelfTestMetadataProvider.GetValidBusContacts(testType);

    /// <inheritdoc />
    public string GetCircuitName(TypeConnector testType, int busContact) => SelfTestMetadataProvider.GetCircuitName(testType, busContact);

    /// <inheritdoc />
    public async Task<int> GetRelayCountAsync(TypeConnector testType, int busContact) => await SelfTestMetadataProvider.GetRelayCountAsync(_deviceBusCommutation, testType, busContact);

    /// <inheritdoc />
    public IEnumerable<object> GetSupportedTestTypes() => SelfTestMetadataProvider.GetSupportedTestTypes();

    /// <inheritdoc />
    public Type GetTestTypeEnum() => SelfTestMetadataProvider.GetTestTypeEnum();

    #endregion

    /// <inheritdoc />
    public Task StartSelfCheck(CancellationToken cancellationToken, IUserMessageService messageService, System.Enum selectedType, ISwitchingDevice device = null, IFastMeter meter = null) => SelfTestProcessManager.StartSelfCheck(cancellationToken, messageService, selectedType, device, meter);

    /// <inheritdoc />
    public async Task<bool> ControlRelayAsync(CancellationToken cancellationToken, TypeConnector testType, int relayNumber, int busContact, int action) => await SelfTestProcessManager.ControlRelayAsync(cancellationToken, _deviceBusCommutation, testType, relayNumber, busContact, action);

  
  }
}

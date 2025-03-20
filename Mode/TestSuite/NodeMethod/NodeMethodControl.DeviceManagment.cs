using Core.Model;

namespace Mode.TestSuite.NodeMethod
{
  /// <summary>
  /// Класс управления методами узла.
  /// </summary>
  public partial class NodeMethodControl
  {
    /// <summary>
    /// Пытается подключиться к устройствам, используя предопределенный список моделей устройств.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию. Возвращает true, если подключение успешно, иначе false.</returns>
    public async Task<bool> AttemptDeviceConnection()
    {
      return await ProtocolSelfCheckControl.AttemptDeviceConnection(
          new List<DeviceModel>
          {
                    testDataModel.ManagerShassy,
                    testDataModel.FirstModuleRelayControl,
                    testDataModel.LastModuleRelayControl,
                    deviceBusCommutation,
                    gptLibrary,
          },
          ShowMessageAsync);
    }

    /// <summary>
    /// Пытается подключиться к указанному списку моделей устройств.
    /// </summary>
    /// <param name="models">Список моделей устройств для подключения.</param>
    /// <returns>Задача, представляющая асинхронную операцию. Возвращает true, если подключение успешно, иначе false.</returns>
    public async Task<bool> AttemptDeviceConnection(List<DeviceModel> models)
    {
      return await ProtocolSelfCheckControl.AttemptDeviceConnection(models, ShowMessageAsync);
    }
  }
}

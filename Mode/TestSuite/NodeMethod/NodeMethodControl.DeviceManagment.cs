using Core.Model;

namespace Mode.TestSuite.NodeMethod
{
  partial class NodeMethodControl
  {
    /// <summary>
    /// Пытается подключиться к устройствам.
    /// </summary>
    public async Task<bool> AttemptDeviceConnection() => await ProtocolSelfCheckControl.AttemptDeviceConnection
    (
    new List<DeviceModel>()
      {
      testDataModel.ManagerShassy,
      testDataModel.FirstModuleRelayControl,
      testDataModel.LastModuleRelayControl,
      deviceBusCommutation,
      gptLibrary,

      }, ShowMessageAsync
      );

    /// <summary>
    /// Пытается подключиться к устройствам.
    /// </summary>
    public async Task<bool> AttemptDeviceConnection(List<DeviceModel> models) => await ProtocolSelfCheckControl.AttemptDeviceConnection(models, ShowMessageAsync);


  }
}

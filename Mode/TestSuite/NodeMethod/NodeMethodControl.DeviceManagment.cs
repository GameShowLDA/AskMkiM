using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Model;
using Mode.Models;

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

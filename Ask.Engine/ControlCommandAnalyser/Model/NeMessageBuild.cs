using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  internal class NeMessageBuild : IDislpayInfo
  {
    public string BuildErrorChainStringAsync(ChainModel chain)
    {
      var chainStr = PointFormater.GetFormatConnectPoint(chain);
      return chainStr;
    }
  }
}

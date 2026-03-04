using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Model.Pr
{
  internal class PrMessageBuild : IDislpayInfo
  {
    public string BuildErrorChainStringAsync(ChainModel chain)
    {
      var chainStr = PointFormater.GetFormatConnectPoint(chain);
      return chainStr;
    }
  }
}

using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model.Pr
{
  internal class PrMessageBuild : IDislpayInfo
  {
    public async Task<string> BuildErrorChainStringAsync(ChainModel chain)
    {
      var chainStr = await PointFormater.GetFormatConnectPoint(chain);
      return chainStr;
    }
  }
}

using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model.Ie
{
  internal class IeMessageBuild : IDislpayInfo
  {
    public string BuildErrorChainStringAsync(ChainModel chain)
    {
      var chainStr = PointFormater.GetFormatConnectPoint(chain);
      return chainStr;
    }
  }
}

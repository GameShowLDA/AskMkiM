using Ask.Core.Services.Config.AppSettings;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model.Ks
{
  public class KsMessageBuild : IDislpayInfo
  {
    public async Task<string> BuildErrorChainStringAsync(ChainModel chain)
    {
      var chainStr = string.Empty;

      for (int z = 0; z < chain.PointModels.Count; z++)
      {
        var pointErr = chain.PointModels[z];
        var machineAdrees = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $" [{pointErr.ToString()}]" : string.Empty;

        chainStr += pointErr.Mnemonic + machineAdrees;

        if (z + 1 < chain.PointModels.Count)
        {
          chainStr += ", ";
        }
      }

      return chainStr;
    }
  }
}

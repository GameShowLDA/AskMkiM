using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Model.Ks
{
  public class KsMessageBuild : IDislpayInfo
  {
    public string BuildErrorChainStringAsync(ChainModel chain)
    {
      var chainStr = string.Empty;

      for (int z = 0; z < chain.PointModels.Count; z++)
      {
        var pointErr = chain.PointModels[z];
        string machineAddress = string.Empty;

        if (DeviceDisplayConfig.GetMachineAddressVisibility())
        {
          if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
          {
            machineAddress =$"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(pointErr.ToString())}]";
          }
          else
          {
            machineAddress =$"[{pointErr.ToString()}]";
          }
        }


        chainStr += pointErr.Mnemonic + machineAddress;

        if (z + 1 < chain.PointModels.Count)
        {
          chainStr += ", ";
        }
      }

      return chainStr;
    }
  }
}

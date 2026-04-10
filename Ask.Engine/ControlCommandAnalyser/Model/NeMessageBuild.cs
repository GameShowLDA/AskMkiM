using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Services.Config.AppSettings;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  internal class NeMessageBuild : IDislpayInfo
  {
    public string BuildErrorChainStringAsync(ChainModel chain)
    {
      var chainStr = string.Empty;

      for (int index = 0; index < chain.PointModels.Count; index++)
      {
        var point = chain.PointModels[index];
        var machineAddress = DeviceDisplayConfig.GetMachineAddressVisibility() ? $" [{point}]" : string.Empty;

        chainStr += point.Mnemonic + machineAddress;

        if (index + 1 < chain.PointModels.Count)
        {
          chainStr += ", ";
        }
      }

      return chainStr;
    }
  }
}

using AppConfig.Config;

namespace AppConfig.Data.Protocol
{
  static internal class ProtocolSettingsManager
  {
    /// <summary>
    /// Считывает параметры отображения данных в протоколе и задаёт их в программе.
    /// </summary>
    static internal async Task ReadProtocolModeAsync()
    {
      ProtocolFileManager protocolFileManager = new ProtocolFileManager(FileLocations.ProtocolConfigPath);

      if (!await protocolFileManager.CreateFileIfNotExistsAsync())
      {
        return;
      }

      ProtocolModel protocolModel = await protocolFileManager.ReadFileAsync();
      if (protocolModel == null)
      {
        return;
      }

      await ConfigModel.SetProtocolModelAsync(protocolModel);
    }
  }
}

using AppConfiguration.Base;
using AppConfiguration.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Parameter
{
  static public class ParameterSettingsManager
  {
    /// <summary>
    /// Считывает параметры отображения данных в протоколе и задаёт их в программе.
    /// </summary>
    static public async Task ReadParameterModeAsync()
    {
      ParameterFileManager protocolFileManager = new ParameterFileManager(FileLocations.ParameterConfigPath);

      if (!await protocolFileManager.CreateFileIfNotExistsAsync())
      {
        return;
      }

      ParameterModel protocolModel = await protocolFileManager.ReadFileAsync();
      if (protocolModel == null)
      {
        return;
      }

      await ConfigModel.SerParametrModelAsync(protocolModel);
    }
  }
}

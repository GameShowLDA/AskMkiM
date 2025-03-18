using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Interface.Main;
using Utilities.Models;
using static Utilities.LoggerUtility;


namespace TestConsole.MINT
{
  public static partial class Mint_Test
  {

    /// <summary>
    /// Проверяет коммутацию МИНТ.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    static private async Task CheckMintSwitching(IFastMeter fastMeter, IPowerSourceModule powerSource, ISwitchingDevice switching)
    {
      await powerSource.StateManager.ResetAsync();
      await switching.StateManager.ResetAsync();
      LogInformation("Начало проверки коммутации МИНТ");

      LogInformation("Завершение проверки коммутации МИНТ");
    }

  }
}

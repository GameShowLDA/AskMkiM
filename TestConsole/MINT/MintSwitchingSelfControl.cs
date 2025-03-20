using NewCore.Base.Interface.Main;
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

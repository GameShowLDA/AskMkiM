using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using static Ask.LogLib.LoggerUtility;


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
      await powerSource.ConnectableManager.ResetAsync();
      await switching.ConnectableManager.ResetAsync();
      LogInformation("Начало проверки коммутации МИНТ");

      LogInformation("Завершение проверки коммутации МИНТ");
    }
  }
}

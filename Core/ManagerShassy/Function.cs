using System.Net;
using Core.Communication;

namespace Core.ManagerShassy
{
  /// <summary>
  /// Методы управления менеджером шасси.
  /// </summary>
  public static class Function
  {
    /// <summary>
    /// Запускает питание на АСК-МКИ-М.
    /// </summary>
    /// <param name="askMkiIp">Ip АСК-МКИ-М.</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public static async Task StartPowerAsync(IPAddress askMkiIp)
    {
      await CommunicationManager.SendCommandAsync(askMkiIp, new Command(2, 1, 1));
    }

    /// <summary>
    /// Выключает питание на АСК-МКИ-М.
    /// </summary>
    /// <param name="askMkiIp">Ip АСК-МКИ-М.</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public static async Task StopPowerAsync(IPAddress askMkiIp)
    {
      await CommunicationManager.SendCommandAsync(askMkiIp, new Command(2, 2, 1));
    }

    /// <summary>
    /// Инициализация устройства коммутации шин.
    /// </summary>
    /// <param name="ipDevice">IP адрес УКШ.</param>
    /// <returns>Кортеж с булевым результатом и строкой, содержащей ответ от инициализации при ошибке.</returns>
    public static async Task<(bool, string)> Initialize(IPAddress ipDevice)
    {
      Command cmd = new Command(1, 0, 0, 0);
      string result = await CommunicationManager.SendCommandAsync(ipDevice, cmd, 2000).ConfigureAwait(true);
      return result == "1.0.1" ? (true, null) : (false, result);
    }
  }
}

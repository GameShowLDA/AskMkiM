using NewCore.Base.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Communication
{
  /// <summary>
  /// Определяет интерфейс для универсального протокола обмена данными с устройствами.
  /// Используется для отправки команд и получения ответов от устройств независимо от способа подключения (IP, COM и т.д.).
  /// </summary>
  public interface IDeviceProtocol
  {
    /// <summary>
    /// Отправляет команду указанному устройству, опционально ожидает завершения операции и получения ответа.
    /// </summary>
    /// <param name="command">Команда для отправки.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа (мс). По умолчанию 0.</param>
    /// <param name="timeout">Таймаут ожидания ответа (мс). Если 0 — ответ не ожидается.</param>
    /// <returns>Ответ от устройства или пустая строка.</returns>
    Task<string> QueryAsync(string command, int responseDelay = 0, int timeout = 0);
  }
}

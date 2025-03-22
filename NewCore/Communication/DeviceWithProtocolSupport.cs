using NewCore.Base.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Communication
{
  /// <summary>
  /// Абстрактный базовый класс для устройств, поддерживающих универсальный протокол обмена данными.
  /// Предоставляет реализацию отправки команд и получения ответов через интерфейс <see cref="IDeviceProtocol"/>.
  /// </summary>
  public abstract class DeviceWithProtocolSupport
  {
    /// <summary>
    /// Свойство подключения к устройству (COM-порт или IP-адрес).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Экземпляр протокола обмена данными, который используется для общения с устройством.
    /// Должен быть инициализирован до вызова <see cref="QueryAsync"/>.
    /// </summary>
    public IDeviceProtocol DeviceProtocol { get; set; }

    /// <summary>
    /// Отправляет команду устройству, опционально ожидает завершения операции и получения ответа.
    /// </summary>
    /// <param name="command">Команда для отправки устройству.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа, в миллисекундах. По умолчанию 0.</param>
    /// <param name="timeout">Таймаут ожидания ответа от устройства, в миллисекундах. Если 0 — ответ не читается.</param>
    /// <returns>Ответ от устройства или пустая строка, если ответ не требуется.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если <see cref="DeviceProtocol"/> не задан.</exception>
    protected async Task<string> QueryAsync(string command, int responseDelay = 0, int timeout = 0)
    {
      if (DeviceProtocol == null)
      {
        throw new InvalidOperationException("Протокол устройства не установлен. Назначьте свойство DeviceProtocol перед вызовом QueryAsync.");
      }

      return await DeviceProtocol.QueryAsync(command, responseDelay, timeout);
    }
  }
}

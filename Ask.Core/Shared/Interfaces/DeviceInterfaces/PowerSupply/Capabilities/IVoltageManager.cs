using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities
{
  /// <summary>
  /// Предоставляет управление уставкой напряжения источника питания.
  /// </summary>
  public interface IVoltageManager
  {
    /// <summary>
    /// Минимальная допустимая уставка напряжения в вольтах.
    /// </summary>
    decimal MinVoltage { get; }

    /// <summary>
    /// Максимальная допустимая уставка напряжения в вольтах.
    /// </summary>
    decimal MaxVoltage { get; }

    /// <summary>
    /// Программный шаг уставки напряжения в вольтах, отсчитываемый от <see cref="MinVoltage"/>.
    /// </summary>
    decimal VoltageStep { get; }

    /// <summary>
    /// Устанавливает напряжение в допустимом диапазоне с программным шагом.
    /// </summary>
    /// <param name="voltage">Уставка напряжения в вольтах, кратная шагу относительно минимальной уставки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Задача установки напряжения.</returns>
    Task SetVoltageAsync(decimal voltage, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Читает уставку напряжения источника питания.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Уставка напряжения в вольтах.</returns>
    Task<decimal> GetVoltageAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null);
  }
}

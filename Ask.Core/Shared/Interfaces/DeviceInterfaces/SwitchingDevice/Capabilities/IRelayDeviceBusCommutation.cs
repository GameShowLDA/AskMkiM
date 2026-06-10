using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities
{
  /// <summary>
  /// Интерфейс для управления реле в УКШ.
  /// </summary>
  public interface IRelayDeviceBusCommutation
  {
    /// <summary>
    /// Подключает реле с указанным номером.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо подключить.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> ConnectRelay(int numberRelay, IUserInteractionService? userMessageService = null);

    //TODO: Надо подумать над новым название для методов EnableRelay и DisableRelay.

    /// <summary>
    /// Отключает реле с указанным номером.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо отключить.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> DisconnectRelay(int numberRelay, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Включить реле.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> EnableRelay(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Выключить реле.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> DisableRelay(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Включение RC реле.
    /// </summary>
    /// <returns>Возвращает <see langword="true"/>, если операция выполнена успешно, иначе <see langword="false"/>.</returns>
    Task<bool> ConnectRCRelay(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Выключить RC реле.
    /// </summary>
    /// <returns>Возвращает <see langword="true"/>, если операция выполнена успешно, иначе <see langword="false"/>.</returns>
    Task<bool> DisconnectRCRelay(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подключить резистор с указанным номером в RC реле.
    /// </summary>
    /// <param name="numberResistor">Порядковый номер резистора.</param>
    /// <returns>Возвращает <see langword="true"/>, если операция выполнена успешно, иначе <see langword="false"/>.</returns>
    /// <remarks>Пример порядкового номера: R1, R2, R3...</remarks>
    Task<bool> ConnectResistor(int numberResistor, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключить резистор с указанным номером в RC реле.
    /// </summary>
    /// <param name="numberResistor">Порядковый номер резистора.</param>
    /// <returns>Возвращает <see langword="true"/>, если операция выполнена успешно, иначе <see langword="false"/>.</returns>
    /// <remarks>Пример порядкового номера: R1, R2, R3...</remarks>
    Task<bool> DisconnectResistor(int numberResistor, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подключить конденсатор с указанным номером в RC реле.
    /// </summary>
    /// <param name="numberCapacitor">Порядковый номер конденсатора.</param>
    /// <returns>Возвращает <see langword="true"/>, если операция выполнена успешно, иначе <see langword="false"/>.</returns>
    /// <remarks>Пример порядкового номера: C1, C2, C3...</remarks>
    Task<bool> ConnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключить конденсатор с указанным номером в RC реле.
    /// </summary>
    /// <param name="numberCapacitor">Порядковый номер конденсатора.</param>
    /// <returns>Возвращает <see langword="true"/>, если операция выполнена успешно, иначе <see langword="false"/>.</returns>
    /// <remarks>Пример порядкового номера: C1, C2, C3...</remarks>
    Task<bool> DisconnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null);
  }
}
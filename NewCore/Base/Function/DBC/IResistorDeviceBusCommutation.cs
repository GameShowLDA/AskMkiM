namespace NewCore.Base.Function.DBC
{
  /// <summary>
  /// Интерфейс для работы с резисторами в устройстве коммутации шин.
  /// Определяет методы подключения и отключения резисторов.
  /// </summary>
  public interface IResistorDeviceBusCommutation
  {
    /// <summary>
    /// Подключает резистор с указанным номером.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача, содержащая результат операции (true, если успешно).</returns>
    Task<bool> ConnectResistor(string number);

    /// <summary>
    /// Отключает резистор с указанным номером.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача, содержащая результат операции (true, если успешно).</returns>
    Task<bool> DisconnectResistor(string number);
  }
}

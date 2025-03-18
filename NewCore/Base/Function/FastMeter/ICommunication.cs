namespace NewCore.Base.Function.FastMeter
{
  /// <summary>
  /// Определяет интерфейс для коммуникации с устройством, включая установку соединения, отправку команд и получение ответов.
  /// </summary>
  public interface ICommunication
  {
    /// <summary>
    /// Асинхронно отправляет команду устройству без ожидания ответа.
    /// </summary>
    /// <param name="command">Команда в строковом формате.</param>
    Task SendCommandAsync(string command);

    /// <summary>
    /// Асинхронно отправляет команду устройству и ожидает ответ.
    /// </summary>
    /// <param name="command">Команда в строковом формате.</param>
    /// <returns>Задача, возвращающая строковый ответ от устройства.</returns>
    Task<string> QueryAsync(string command);
  }

}

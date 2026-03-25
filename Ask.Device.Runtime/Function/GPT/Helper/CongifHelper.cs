namespace Ask.Device.Runtime.Function.GPT.Helper
{
  /// <summary>
  /// Вспомогательный класс для унифицированной установки параметров устройства.
  /// Позволяет сократить дублирование кода при работе с методами вида "SetXxxAsync".
  /// </summary>
  static internal class CongifHelper
  {
    /// <summary>
    /// Универсальный метод для установки параметра устройства с проверкой текущего значения.
    /// </summary>
    /// <typeparam name="T">Тип параметра (например, <see cref="double"/> или <see cref="int"/>).</typeparam>
    /// <param name="getter">
    /// Асинхронная функция для получения текущего значения параметра.
    /// Обычно реализуется как вызов метода <c>GetXxxAsync()</c>.
    /// </param>
    /// <param name="setter">
    /// Асинхронная функция для установки нового значения параметра.
    /// Обычно реализуется как вызов метода <c>Helper.SetXxxAsync(...)</c>.
    /// </param>
    /// <param name="updateConfig">
    /// Действие для обновления локальной конфигурации после успешной установки параметра.
    /// Например, присвоение значения в объекте <c>_config</c>.
    /// </param>
    /// <param name="newValue">
    /// Новое значение параметра, которое требуется установить.
    /// </param>
    /// <returns>
    /// Кортеж:
    /// <list type="bullet">
    /// <item><description><c>Success</c> — <c>true</c>, если установка прошла успешно или значение уже было установлено;</description></item>
    /// <item><description><c>Message</c> — диагностическое сообщение или пустая строка.</description></item>
    /// </list>
    /// </returns>
    static public async Task<(bool Success, string Message)> SetParameterAsync<T>(
      Func<Task<T>> getter,
      Func<Task<(bool Success, string Message)>> setter,
      Action<T> updateConfig,
      T newValue)
    {
      var current = await getter();
      if (EqualityComparer<T>.Default.Equals(current, newValue))
        return (true, string.Empty);

      var result = await setter();
      if (result.Success)
        updateConfig(newValue);

      return result;
    }
  }
}

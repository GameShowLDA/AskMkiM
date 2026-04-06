namespace Ask.Core.Shared.DTO.Devices.Base
{
  public class DeviceCommandInfo
  {
    /// <summary>
    /// Идентификатор команды (номер функции устройства).
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Человекочитаемое название команды.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Синтаксис команды, отправляемой устройству.
    /// </summary>
    public string Syntax { get; init; } = string.Empty;

    /// <summary>
    /// Описание входных параметров команды.
    /// Если параметры отсутствуют, указывается "-".
    /// </summary>
    public string Variables { get; init; } = "-";

    /// <summary>
    /// Описание формата ответа устройства.
    /// </summary>
    public string Response { get; init; } = "-";

    /// <summary>
    /// Пример ответа устройства для данной команды.
    /// Используется в справке для наглядности.
    /// </summary>
    public string ResponseExample { get; init; } = "-";
  }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewCore.Base.DeviceResponses
{
  /// <summary>
  /// Базовый класс ответа от устройства.
  /// </summary>
  public class BaseResponse
  {
    /// <summary>
    /// Название модуля.
    /// </summary>
    [JsonPropertyName("ModuleName")]
    public string ModuleName { get; set; }

    /// <summary>
    /// Номер устройства.
    /// </summary>
    [JsonPropertyName("NumberDevice")]
    public int NumberDevice { get; set; }

    /// <summary>
    /// Номер шасси.
    /// </summary>
    [JsonPropertyName("NumberChassis")]
    public int NumberChassis { get; set; }

    /// <summary>
    /// Ответ от устройства.
    /// </summary>
    [JsonPropertyName("Answer")]
    public string? Answer { get; set; }

    /// <summary>
    /// Десериализует JSON-строку в объект <see cref="BaseResponse"/>.
    /// </summary>
    /// <param name="json">Строка JSON с данными.</param>
    /// <returns>Экземпляр <see cref="BaseResponse"/> с заполненными данными.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="json"/> равен null или пустой строке.</exception>
    /// <exception cref="JsonException">Выбрасывается, если произошла ошибка десериализации.</exception>
    public static BaseResponse FromJson(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        throw new ArgumentNullException(nameof(json), "JSON-строка не должна быть null или пустой.");
      }

      try
      {
        return JsonSerializer.Deserialize<BaseResponse>(json);
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}

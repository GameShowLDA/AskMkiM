using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NewCore.Base.DeviceResponses
{
  internal class RelayVerifiedAnswer : BaseResponse
  {
    /// <summary>
    /// Название модуля.
    /// </summary>
    [JsonPropertyName("Checked")]
    public bool Checked { get; set; }

    /// <summary>
    /// Десериализует JSON-строку в объект <see cref="RelayVerifiedAnswer"/>.
    /// </summary>
    /// <param name="json">Строка JSON с данными.</param>
    /// <returns>Экземпляр <see cref="RelayVerifiedAnswer"/> с заполненными данными.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="json"/> равен null или пустой строке.</exception>
    /// <exception cref="JsonException">Выбрасывается, если произошла ошибка десериализации.</exception>
    public static RelayVerifiedAnswer FromJson(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        throw new ArgumentNullException(nameof(json), "JSON-строка не должна быть null или пустой.");
      }

      try
      {
        return JsonSerializer.Deserialize<RelayVerifiedAnswer>(json);
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}

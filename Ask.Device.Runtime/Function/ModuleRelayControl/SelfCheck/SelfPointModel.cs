using Newtonsoft.Json;

namespace Ask.Device.Runtime.Function.ModuleRelayControl.SelfCheck
{
  /// <summary>
  /// Модель, описывающая параметры точки самоконтроля модуля реле.
  /// </summary>
  internal class SelfPointModel
  {
    /// <summary>
    /// Номер устройства.
    /// </summary>
    [JsonProperty("NumberDevice")]
    public int NumberDevice { get; set; }

    /// <summary>
    /// Номер точки.
    /// </summary>
    [JsonProperty("NumberPoint")]
    public int NumberPoint { get; set; }

    /// <summary>
    /// Флаг подключения точки.
    /// </summary>
    [JsonProperty("ConnectPoint")]
    public bool ConnectPoint { get; set; }

    /// <summary>
    /// Флаг отключения шины A.
    /// </summary>
    [JsonProperty("DisconnectBusA")]
    public bool DisconnectBusA { get; set; }

    /// <summary>
    /// Флаг отключения шины B.
    /// </summary>
    [JsonProperty("DisconnectBusB")]
    public bool DisconnectBusB { get; set; }

    /// <summary>
    /// Флаг, указывающий, что самоконтроль пройден.
    /// </summary>
    [JsonProperty("SelfControl")]
    public bool SelfControl { get; set; }

    /// <summary>
    /// Преобразует JSON-строку в объект SelfPointModel.
    /// </summary>
    /// <param name="json">JSON-строка с данными.</param>
    /// <returns>Объект SelfPointModel или null, если преобразование не удалось.</returns>
    public static SelfPointModel FromJson(string json)
    {
      try
      {
        var result = JsonConvert.DeserializeObject<SelfPointModel>(json);
        return result;
      }
      catch (JsonException ex)
      {
        Console.WriteLine($"Ошибка преобразования JSON: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// Преобразует объект SelfPointModel в форматированную JSON-строку.
    /// </summary>
    /// <returns>JSON-строка, представляющая объект.</returns>
    public string ToJson()
    {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
  }
}

using Newtonsoft.Json;

namespace Ask.Device.Runtime.Function.ModuleRelayControl.SelfCheck
{
  internal class SelfBusModel
  {
    /// <summary>
    /// Номер устройства.
    /// </summary>
    [JsonProperty("NumberDevice")]
    public int NumberDevice { get; set; }

    /// <summary>
    /// Номер шасси.
    /// </summary>
    [JsonProperty("NumberChassis")]
    public int NumberChassis { get; set; }

    /// <summary>
    /// Номер шины.
    /// </summary>
    [JsonProperty("NumberBus")]
    public int NumberBus { get; set; }

    /// <summary>
    /// Защитное реле для подлючения шины А.
    /// </summary>
    [JsonProperty("ProtectReleBusA")]
    public int ProtectReleBusA { get; set; }

    /// <summary>
    /// Защитное реле для подлючения шины B.
    /// </summary>
    [JsonProperty("ProtectReleBusB")]
    public int ProtectReleBusB { get; set; }

    /// <summary>
    /// результыт проверки защитных реле.
    /// </summary>
    [JsonProperty("ConnectProtect")]
    public bool ConnectProtect { get; set; }

    /// <summary>
    /// Основное реле для подлючения шины А.
    /// </summary>
    [JsonProperty("MainReleBusA")]
    public int MainReleBusA { get; set; }

    /// <summary>
    /// Основное реле для подлючения шины B.
    /// </summary>
    [JsonProperty("MainReleBusB")]
    public int MainReleBusB { get; set; }

    /// <summary>
    /// результыт проверки основных реле.
    /// </summary>
    [JsonProperty("ConnectMain")]
    public bool ConnectMain { get; set; }

    /// <summary>
    /// Преобразует JSON-строку в объект SelfBusModel.
    /// </summary>
    /// <param name="json">JSON-строка с данными.</param>
    /// <returns>Объект SelfPointModel или null, если преобразование не удалось.</returns>
    public static SelfBusModel FromJson(string json)
    {
      try
      {
        var result = JsonConvert.DeserializeObject<SelfBusModel>(json);
        return result;
      }
      catch (JsonException ex)
      {
        Console.WriteLine($"Ошибка преобразования JSON: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// Преобразует объект SelfBusModel в форматированную JSON-строку.
    /// </summary>
    /// <returns>JSON-строка, представляющая объект.</returns>
    public string ToJson()
    {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
  }
}

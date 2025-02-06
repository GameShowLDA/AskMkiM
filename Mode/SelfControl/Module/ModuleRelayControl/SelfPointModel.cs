using Newtonsoft.Json;

namespace Mode.SelfControl.Module.ModuleRelayControl
{
  internal class SelfPointModel
  {
    [JsonProperty("NumberDevice")]
    public int NumberDevice { get; set; }

    [JsonProperty("NumberPoint")]
    public int NumberPoint { get; set; }

    [JsonProperty("ConnectPoint")]
    public bool ConnectPoint { get; set; }

    [JsonProperty("DisconnectBusA")]
    public bool DisconnectBusA { get; set; }

    [JsonProperty("DisconnectBusB")]
    public bool DisconnectBusB { get; set; }

    [JsonProperty("SelfControl")]
    public bool SelfControl { get; set; }

    public static SelfPointModel FromJson(string json)
    {
      try
      {
        var result = JsonConvert.DeserializeObject<SelfPointModel>(json);

        Console.WriteLine($"Принимаемая строка: {json}");
        Console.WriteLine($"NumberDevice: {result.NumberDevice}");
        Console.WriteLine($"NumberPoint: {result.NumberPoint}");
        Console.WriteLine($"ConnectPoint: {result.ConnectPoint}");
        Console.WriteLine($"DisconnectBusA: {result.DisconnectBusA}");
        Console.WriteLine($"DisconnectBusB: {result.DisconnectBusB}");
        Console.WriteLine($"SelfControl: {result.SelfControl}");
        return result;
      }
      catch (JsonException ex)
      {
        Console.WriteLine($"Ошибка преобразования JSON: {ex.Message}");
        return null;
      }


    }

    // Метод для преобразования объекта в JSON-строку
    public string ToJson()
    {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
  }
}

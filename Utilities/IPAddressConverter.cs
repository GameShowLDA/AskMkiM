using System.Net;
using Newtonsoft.Json;

namespace Utilities
{
  /// <summary>
  /// Конвертер для сериализации и десериализации объектов типа IPAddress в JSON.
  /// Этот класс наследуется от JsonConverter и переопределяет методы для
  /// преобразования IPAddress в строку и обратно при работе с JSON.
  /// </summary>
  internal class IPAddressConverter : JsonConverter
  {
    /// <summary>
    /// Определяет, может ли конвертер преобразовать указанный тип объекта.
    /// </summary>
    /// <param name="objectType">Тип объекта для проверки.</param>
    /// <returns>True, если объект типа IPAddress; в противном случае - false.</returns>
    public override bool CanConvert(Type objectType)
    {
      return (objectType == typeof(IPAddress));
    }

    /// <summary>
    /// Записывает значение IPAddress в JSON.
    /// </summary>
    /// <param name="writer">Объект JsonWriter для записи JSON.</param>
    /// <param name="value">Значение для записи.</param>
    /// <param name="serializer">Объект JsonSerializer для сериализации.</param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.WriteValue(value.ToString());
    }

    /// <summary>
    /// Читает значение IPAddress из JSON.
    /// </summary>
    /// <param name="reader">Объект JsonReader для чтения JSON.</param>
    /// <param name="objectType">Тип объекта для десериализации.</param>
    /// <param name="existingValue">Существующее значение объекта.</param>
    /// <param name="serializer">Объект JsonSerializer для десериализации.</param>
    /// <returns>Объект IPAddress, созданный из строкового представления.</returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      return IPAddress.Parse((string)reader.Value);
    }
  }
}

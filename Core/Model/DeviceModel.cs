using System.Net;
using Core.Enum;
using Newtonsoft.Json.Linq;

namespace Core.Model
{
  /// <summary>
  /// Модель подключаемого устройства к системе.
  /// </summary>
  public class DeviceModel
  {
    /// <summary>
    /// Gets or sets тип устройства.
    /// </summary>
    public DeviceEnum.Type DeviceType { get; set; }

    /// <summary>
    /// Gets or sets id устройства.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets имя устройства.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets описание устройства.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets iP - Адрес устройства.
    /// </summary>
    public IPAddress IPAddress { get; set; }

    /// <summary>
    /// Gets or sets номер устройства.
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether флаг, указывающий, активен ли модуль устройства.
    /// </summary>
    public bool ModuleActive { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceModel"/> class.
    /// Конструктор по умолчанию.
    /// </summary>
    public DeviceModel()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceModel"/> class.
    /// Конструктор для создания экземпляра устройства с указанными параметрами.
    /// </summary>
    /// <param name="type">Тип устройства.</param>
    /// <param name="name">Имя устройства.</param>
    /// <param name="description">Описание устройства.</param>
    /// <param name="ip">IP-адрес устройства.</param>
    /// <param name="number">Номер устройства.</param>
    /// <param name="moduleActive">Флаг, указывающий, активен ли модуль устройства.</param>
    public DeviceModel(DeviceEnum.Type type, string name, string description, IPAddress ip, string number, bool moduleActive)
    {
      this.DeviceType = type;
      this.Name = name;
      this.Description = description;
      this.IPAddress = ip;
      this.Number = number;
      this.ModuleActive = moduleActive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceModel"/> class.
    /// Конструктор для создания экземпляра устройства с указанными параметрами.
    /// </summary>
    /// <param name="type">Тип устройства.</param>
    /// <param name="name">Имя устройства.</param>
    /// <param name="description">Описание устройства.</param>
    /// <param name="number">Номер устройства.</param>
    /// <param name="moduleActive">Флаг, указывающий, активен ли модуль устройства.</param>
    public DeviceModel(DeviceEnum.Type type, string name, string description, string number, bool moduleActive)
    {
      this.DeviceType = type;
      this.Name = name;
      this.Description = description;
      this.Number = number;
      this.ModuleActive = moduleActive;
    }

    /// <summary>
    /// Пытается извлечь DeviceEnum.Type из объекта.
    /// </summary>
    /// <param name="obj">Объект для анализа.</param>
    /// <returns>DeviceEnum.Type, если найден; иначе null.</returns>
    public static DeviceEnum.Type? TryGetDeviceTypeFromObject(object obj)
    {
      if (obj == null)
      {
        return null;
      }

      // Если объект - строка, пробуем распарсить его как JSON
      if (obj is string jsonString)
      {
        try
        {
          obj = JObject.Parse(jsonString);
        }
        catch (Exception)
        {
          // Если не удалось распарсить как JSON, продолжаем с исходным объектом
        }
      }

      // Если объект - JObject (распарсенный JSON), ищем в нем DeviceType
      if (obj is JObject jObject)
      {
        if (jObject.TryGetValue("DeviceType", out JToken deviceTypeToken))
        {
          if (int.TryParse(deviceTypeToken.ToString(), out int deviceTypeInt))
          {
            return (DeviceEnum.Type)deviceTypeInt;
          }
        }
      }

      // Проверяем, является ли объект сам по себе DeviceEnum.Type
      if (obj is DeviceEnum.Type deviceType)
      {
        return deviceType;
      }

      // Ищем свойство с типом DeviceEnum.Type или int (который может быть преобразован в DeviceEnum.Type)
      var property = obj.GetType().GetProperties()
        .FirstOrDefault(p => p.PropertyType == typeof(DeviceEnum.Type) || p.PropertyType == typeof(int));

      if (property != null)
      {
        var value = property.GetValue(obj);
        if (value is DeviceEnum.Type type)
        {
          return type;
        }

        if (value is int intValue)
        {
          return (DeviceEnum.Type)intValue;
        }
      }

      // Если не нашли, возвращаем null
      return null;
    }
  }
}

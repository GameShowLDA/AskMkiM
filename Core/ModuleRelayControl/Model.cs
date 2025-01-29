using System.Net;
using Core.Enum;
using Core.Model;
using Newtonsoft.Json.Linq;

namespace Core.ModuleRelayControl
{
  /// <summary>
  /// Класс, представляющий модуля коммутации реле.
  /// </summary>
  public class Model : DeviceModel
  {
    /// <summary>
    /// Gets or sets кол-во точек блока.
    /// </summary>
    public int CountPoints { get; set; }

    /// <summary>
    /// Gets or sets тип напряжения.
    /// </summary>
    public DeviceEnum.VoltageType VoltageType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    /// <param name="iPAddress">ip адрес МКР.</param>
    public Model(IPAddress iPAddress)
    {
      this.IPAddress = iPAddress;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// Конструктор для создания экземпляра МКР с указанными параметрами.
    /// </summary>
    /// <param name="id">Уникальный номер устройства.</param>
    /// <param name="type">Тип устройства.</param>
    /// <param name="name">Имя устройства.</param>
    /// <param name="description">Описание устройства.</param>
    /// <param name="ip">IP-адрес устройства.</param>
    /// <param name="number">Номер блока.</param>
    /// <param name="moduleActive">Флаг, указывающий, активен ли модуль устройства.</param>
    /// <param name="count">Количество точек.</param>
    /// <param name="voltageType">Тип напряжения.</param>
    public Model(DeviceEnum.Type type, string name, string description, IPAddress ip, string number, bool moduleActive, int count, DeviceEnum.VoltageType voltageType)
       : this(ip)
    {
      this.DeviceType = type;
      this.Name = name;
      this.Description = description;
      this.Number = number;
      this.ModuleActive = moduleActive;
      this.CountPoints = count;
      this.VoltageType = voltageType;
    }

    /// <summary>
    /// Создает экземпляр Model из объекта.
    /// </summary>
    /// <param name="obj">Объект для преобразования.</param>
    /// <returns>Новый экземпляр Model.</returns>
    public static Model CreateFromObject(object obj)
    {
      if (obj == null)
      {
        return null;
      }

      Model model = obj as Model;
      if (model != null)
      {
        return model;
      }

      JObject jObject;
      if (obj is string jsonString)
      {
        jObject = JObject.Parse(jsonString);
      }
      else if (obj is JObject)
      {
        jObject = (JObject)obj;
      }
      else
      {
        throw new ArgumentException("Object must be a JSON string or JObject", nameof(obj));
      }

      return new Model(
        (DeviceEnum.Type)jObject["DeviceType"].Value<int>(),
        jObject["Name"].Value<string>(),
        jObject["Description"].Value<string>(),
        IPAddress.Parse(jObject["IPAddress"].Value<string>()),
        jObject["Number"].Value<string>(),
        jObject["ModuleActive"].Value<bool>(),
        jObject[nameof(CountPoints)].Value<int>(),
        (DeviceEnum.VoltageType)jObject[nameof(VoltageType)].Value<int>());
    }
  }
}

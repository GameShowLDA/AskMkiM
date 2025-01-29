using System.Net;
using Core.Enum;
using Core.Model;
using Newtonsoft.Json.Linq;

namespace Core.Abstract
{
  /// <summary>
  /// Класс, представляющий стандартные методы мультиметров.
  /// </summary>
  public abstract class MeterBase : DeviceModel
  {
    /// <summary>
    /// Проверяет соединение с устройством.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на наличие соединения.
    /// </returns>
    public abstract bool CheckConnection();

    /// <summary>
    /// Асинхронно устанавливает соединение с устройством.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="Task{bool}"/>, представляющий асинхронную операцию. 
    /// Результат указывает, было ли успешно установлено соединение.
    /// </returns>
    public abstract Task<bool> ConnectAsync();

    /// <summary>
    /// Разрывает соединение с устройством.
    /// </summary>
    public abstract void Disconnect();

    /// <summary>
    /// Измеряет непрерывность электрической цепи.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="double"/>, представляющее измеренное значение непрерывности.
    /// </returns>
    public abstract double MeasureContinuity();

    /// <summary>
    /// Измеряет электрическое сопротивление.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="double"/>, представляющее измеренное значение сопротивления.
    /// </returns>
    public abstract double MeasureResistance();

    /// <summary>
    /// Измерение напряжения постоянного тока (DC).
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="double"/>, представляющее измеренное напряжение DC.
    /// </returns>
    public abstract double MeasureVoltageDC();

    /// <summary>
    /// Измерение ёмкости.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="double"/>, представляющее измеренную ёмкость.
    /// </returns>
    public abstract double MeasureCapacitance();

    /// <summary>
    /// Отчистка буфера.
    /// </summary>
    public abstract void ClearBuffer();

    /// <summary>
    /// Устанавливает мультиметр в режим измерения сопротивления.
    /// </summary>
    public abstract void SetResistanceMode();


    /// <summary>
    /// Устанавливает мультиметр в режим измерения ёмкости.
    /// </summary>
    public abstract void SetCapacitanceMode();

    /// <summary>
    /// Статический фабричный метод для создания экземпляра MeterBase или его производных классов.
    /// </summary>
    /// <typeparam name="T">Тип создаваемого устройства, должен быть производным от MeterBase.</typeparam>
    /// <param name="type">Тип устройства.</param>
    /// <param name="name">Имя устройства.</param>
    /// <param name="description">Описание устройства.</param>
    /// <param name="ip">IP-адрес устройства.</param>
    /// <param name="number">Номер устройства.</param>
    /// <param name="moduleActive">Флаг активности модуля устройства.</param>
    /// <returns>Новый экземпляр устройства типа T.</returns>
    public static T CreateMeter<T>(DeviceEnum.Type type, string name, string description, System.Net.IPAddress ip, string number, bool moduleActive)
      where T : MeterBase, new()
    {
      T meter = new T();
      meter.DeviceType = type;
      meter.Name = name;
      meter.Description = description;
      meter.IPAddress = ip;
      meter.Number = number;
      meter.ModuleActive = moduleActive;
      return meter;
    }

    /// <summary>
    /// Создает экземпляр MeterBase из объекта.
    /// </summary>
    /// <param name="obj">Объект для преобразования.</param>
    /// <returns>Новый экземпляр Model.</returns>
    public static DeviceModel CreateFromObject(object obj)
    {
      if (obj == null)
      {
        return null;
      }

      MeterBase model = obj as MeterBase;
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
        return null;
      }

      return new DeviceModel(
        (DeviceEnum.Type)jObject["DeviceType"].Value<int>(),
        jObject["Name"].Value<string>(),
        jObject["Description"].Value<string>(),
        IPAddress.Parse(jObject["IPAddress"].Value<string>()),
        jObject["Number"].Value<string>(),
        jObject["ModuleActive"].Value<bool>());
    }
  }
}

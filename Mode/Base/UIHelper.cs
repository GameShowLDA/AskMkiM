using UI.Components;
using UI.Controls.ProtocolNew;
using static NewCore.Enum.DeviceEnum;

namespace Mode.Base
{
  /// <summary>
  /// Вспомогательный класс для безопасной работы с элементами UI.
  /// </summary>
  public static class UIHelper
  {
    /// <summary>
    /// Безопасно извлекает значения из InputField, независимо от вызывающего потока.
    /// </summary>
    /// <param name="inputField">Экземпляр InputField.</param>
    /// <returns>Кортеж с первой точкой, второй точкой и электрическим параметром.</returns>
    public static (string First, string Second, string Parameter) GetInputFieldValuesSafe(this InputField inputField)
    {
      string first = string.Empty;
      string second = string.Empty;
      string param = string.Empty;

      void ReadValues()
      {
        first = inputField.FirstPoint;
        second = inputField.SecondPoint;
        param = inputField.ElectricalParameter;
      }

      if (inputField.Dispatcher.CheckAccess())
      {
        ReadValues();
      }
      else
      {
        inputField.Dispatcher.Invoke(ReadValues);
      }

      return (first, second, param);
    }

    /// <summary>
    /// Безопасно извлекает значения времени из InputField, независимо от вызывающего потока.
    /// </summary>
    /// <param name="inputField">Экземпляр InputField.</param>
    /// <returns>Кортеж с первой точкой, второй точкой и электрическим параметром.</returns>
    public static string GetInputFieldTimeValuesSafe(this InputField inputField)
    {
      string time = string.Empty;

      void ReadValues()
      {
        time = inputField.Time;
      }

      if (inputField.Dispatcher.CheckAccess())
      {
        ReadValues();
      }
      else
      {
        inputField.Dispatcher.Invoke(ReadValues);
      }

      return time;
    }

    /// <summary>
    /// Безопасно извлекает значения времени из InputField, независимо от вызывающего потока.
    /// </summary>
    /// <param name="inputField">Экземпляр InputField.</param>
    /// <returns>Кортеж с первой точкой, второй точкой и электрическим параметром.</returns>
    public static string GetInputFieldTimeRampValuesSafe(this InputField inputField)
    {
      string time = string.Empty;

      void ReadValues()
      {
        time = inputField.TimeRamp;
      }

      if (inputField.Dispatcher.CheckAccess())
      {
        ReadValues();
      }
      else
      {
        inputField.Dispatcher.Invoke(ReadValues);
      }

      return time.Replace('.', ',');
    }

    /// <summary>
    /// Безопасно извлекает значения напряжения из InputField, независимо от вызывающего потока.
    /// </summary>
    /// <param name="inputField">Экземпляр InputField.</param>
    /// <returns>Кортеж с первой точкой, второй точкой и электрическим параметром.</returns>
    public static string GetInputFieldVoltageValuesSafe(this InputField inputField)
    {
      string voltage = string.Empty;

      void ReadValues()
      {
        voltage = inputField.Voltage;
      }

      if (inputField.Dispatcher.CheckAccess())
      {
        ReadValues();
      }
      else
      {
        inputField.Dispatcher.Invoke(ReadValues);
      }

      return voltage;
    }

    /// <summary>
    /// Безопасно извлекает значения шины из InputField, независимо от вызывающего потока.
    /// </summary>
    /// <param name="inputField">Экземпляр InputField.</param>
    /// <returns>Кортеж с первой точкой, второй точкой и электрическим параметром.</returns>
    public static BusPoint GetInputFieldBusValuesSafe(this InputField inputField)
    {
      BusPoint bus = default;

      void ReadValues()
      {
        bus = inputField.ActiveBus;
      }

      if (inputField.Dispatcher.CheckAccess())
      {
        ReadValues();
      }
      else
      {
        inputField.Dispatcher.Invoke(ReadValues);
      }

      return bus;
    }

    /// <summary>
    /// Безопасно извлекает InputField из ProtocolUI.ContentView.
    /// </summary>
    /// <param name="protocolUI">Элемент ProtocolUI.</param>
    /// <returns>InputField или null, если не удалось извлечь.</returns>
    public static InputField? GetInputFieldSafe(this ProtocolUI protocolUI)
    {
      if (protocolUI == null)
      {
        return null;
      }

      InputField? result = null;

      void TryGet()
      {
        if (protocolUI.ContentView is InputField inputField)
        {
          result = inputField;
        }
      }

      if (protocolUI.Dispatcher.CheckAccess())
      {
        TryGet();
      }
      else
        protocolUI.Dispatcher.Invoke(TryGet);

      return result;
    }
  }
}

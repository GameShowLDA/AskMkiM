using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Components;
using UI.Controls.Protocol;

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
    public static (string First, string Second, string Parameter) GetInputFieldValuesSafe(this InputFieldMultimeter inputField)
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
    /// Безопасно извлекает InputField из ProtocolUI.ContentView.
    /// </summary>
    /// <param name="protocolUI">Элемент ProtocolUI.</param>
    /// <returns>InputField или null, если не удалось извлечь.</returns>
    public static InputFieldMultimeter? GetInputFieldSafe(this ProtocolUI protocolUI)
    {
      if (protocolUI == null)
      {
        return null;
      }

      InputFieldMultimeter? result = null;

      void TryGet()
      {
        if (protocolUI.ContentView is InputFieldMultimeter inputField)
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

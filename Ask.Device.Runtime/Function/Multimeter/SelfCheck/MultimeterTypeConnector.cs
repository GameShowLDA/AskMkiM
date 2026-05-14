using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Device.Runtime.Function.Multimeter.SelfCheck
{
  /// <summary>
  /// Тип проверки мультиметра.
  /// </summary>
  public enum MultimeterTypeConnector
  {
    /// <summary>
    /// Полная проверка мультиметра.
    /// </summary>
    /// <remarks>
    /// За раз проверяет точность измерения напряжения, сопротивления и ёмкости.
    /// </remarks>
    [Description("Полная проверка устройства")]
    FullCheck = 0,

    /// <summary>
    /// Проверяется напряжение на входе мультиметра.
    /// </summary>
    /// <remarks>
    /// Устанавливается режим измерения напряжения  на диапазон 1В, подключается к шинам AB1, замерят результат и сравнят с допуском.
    /// </remarks>
    [Description("Проверка напряжения")]
    Voltage = 1,

    /// <summary>
    /// Проверяет сопротивление на входе мультиметра.
    /// </summary>
    /// <remarks>
    /// Устанавливается режим сопротивления 100 Ом, подключается к шинам AB2, замерят результат и сравнят с допуском.
    /// </remarks>
    [Description("Проверка сопротивления")]
    Resistance = 2,

    /// <summary>
    ///  Проверяет ёмкости на входе мультиметра.
    /// </summary>
    /// <remarks>
    /// Устанавливается режим ёмкости 100 мкФ, подключается к шинам AB4, замерят результат и сравнят с допуском.
    /// </remarks>
    [Description("Проверка ёмкости")]
    Capacity = 3
  }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Base.Function.ModuleRelayControl
{
  /// <summary>
  /// Тип проверки цепи самоконтроля.
  /// </summary>
  public enum TypeConnector
  {
    /// <summary>
    /// Полная проверка всех цепей устройства самоконтроля.
    /// Используется для последовательного запуска всех поддерживаемых тестов.
    /// </summary>
    [Description("Полная проверка устройства")]
    FullCheck = 0,

    /// <summary>
    /// Аналого-цифровой преобразователь (АЦП), используется для измерения входного сигнала.
    /// Подключение: разъем XS3.
    /// </summary>
    [Description("Проверка точек")]
    Points = 1,

    /// <summary>
    /// Аналого-цифровой преобразователь (АЦП) с переполюсовкой,
    /// предназначен для измерения сигнала с измененной полярностью.
    /// Подключение: разъем XS3.
    /// </summary>
    [Description("Проверка коммутации шин")]
    BusCommutation = 2,
  }
}

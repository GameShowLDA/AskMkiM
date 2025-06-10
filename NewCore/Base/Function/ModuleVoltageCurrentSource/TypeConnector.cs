using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Base.Function.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Тип проверки цепи самоконтроля.
  /// </summary>
  public enum TypeConnector
  {
    /// <summary>
    /// Полная проверка всех частей устройства самоконтроля.
    /// Используется для последовательного запуска всех поддерживаемых тестов.
    /// </summary>
    [Description("Полная проверка устройства")]
    FullCheck = 0,

    /// <summary>
    /// Проверка выходного напряжения устройства.
    /// Используется для проверки правильности формирования выходных напряжений в установленных режимах.
    /// </summary>
    [Description("Проверка выходного напряжения")]
    OutputVoltageCheck = 1,

    /// <summary>
    /// Проверка цепей коммутации устройства.
    /// Включает проверку целостности, правильности подключения и переключения цепей.
    /// </summary>
    [Description("Проверка коммутации")]
    CommutationCheck = 2,

    /// <summary>
    /// Проверка цепей коммутации устройства.
    /// Включает проверку целостности, правильности подключения и переключения цепей.
    /// </summary>
    [Description("Проверка выходного тока")]
    OutputCurrentCheck = 3,
  }
}

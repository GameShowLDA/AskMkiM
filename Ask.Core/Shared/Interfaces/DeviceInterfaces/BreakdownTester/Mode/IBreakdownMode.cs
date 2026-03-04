using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode
{
  /// <summary>
  /// Базовый интерфейс режима пробойной установки.
  /// Содержит общие возможности, доступные во всех режимах (ACW, DCW, IR).
  /// </summary>
  public interface IBreakdownMode<TConfig> where TConfig : class
  {
    /// <summary>
    /// Тип режима работы устройства (ACW, DCW, IR и т.д.).
    /// </summary>
    BreakdownTypeMode ModeType { get; }

    /// <summary>
    /// Управление режимом работы устройства.
    /// </summary>
    IModeConfigurable Mode { get; set; }

    /// <summary>
    /// Управление напряжением.
    /// </summary>
    IVoltageConfigurable Voltage { get; set; }

    /// <summary>
    /// Управление временными параметрами.
    /// </summary>
    ITimeConfigurable Time { get; set; }

    /// <summary>
    /// Управление параметром смещения.
    /// </summary>
    IOffsetConfigurable Offset { get; set; }

    /// <summary>
    /// Интерфейс измерений.
    /// </summary>
    IMeasurable Measure { get; set; }

    /// <summary>
    /// Провайдер конфигурации текущего режима.
    /// Обеспечивает доступ к параметрам режима (<typeparamref name="TConfig"/>),
    /// а также операции чтения и сброса конфигурации к значениям по умолчанию.
    /// </summary>
    IConfigurationProvider<TConfig> Config { get; set; }
  }
}

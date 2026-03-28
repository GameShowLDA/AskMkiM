using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces
{
  /// <summary>
  /// Определяет контракт для типобезопасного преобразования устройства в DTO.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <typeparam name="TDto">Тип DTO.</typeparam>
  public interface IDeviceToDtoConverter<out TDto>
  {
    /// <summary>
    /// Преобразует устройство в DTO.
    /// </summary>
    /// <param name="device">Экземпляр устройства.</param>
    /// <returns>DTO, соответствующий устройству.</returns>
    TDto Convert();
  }
}

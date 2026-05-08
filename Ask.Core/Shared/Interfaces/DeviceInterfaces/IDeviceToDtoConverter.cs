using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces;

/// <summary>
/// Определяет контракт для типобезопасного преобразования устройства в DTO.
/// </summary>
/// <typeparam name="TDto">Тип DTO.</typeparam>
public interface IDeviceToDtoConverter<out TDto>
{
  /// <summary>
  /// Преобразует текущее устройство в DTO.
  /// </summary>
  /// <returns>DTO, соответствующий текущему устройству.</returns>
  TDto Convert();
}

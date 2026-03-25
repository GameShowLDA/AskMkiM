using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Core.Shared.DTO.Devices.ChassisManager;

/// <summary>
/// DTO менеджера шасси.
/// Содержит только данные, необходимые для передачи между слоями,
/// без логики управления и зависимостей.
/// </summary>
public class ChassisManagerDto : DeviceDto
{
  /// <summary>
  /// Тип структурной шины тестера.
  /// </summary>
  public BusStructureEnum.Type BusType { get; set; }
}

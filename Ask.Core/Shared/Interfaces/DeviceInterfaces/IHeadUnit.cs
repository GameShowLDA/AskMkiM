using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces
{
  /// <summary>
  /// Интерфейс, представляющий головное устройство, наследуемое от <see cref="IDevice"/>.
  /// </summary>
  public interface IHeadUnit : IDevice 
  {
    /// <summary>
    /// Тип структурной шины тестера АСК.
    /// </summary>
    BusStructureEnum.Type BusType { get; set; }
  }
}

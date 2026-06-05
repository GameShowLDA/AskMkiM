using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using static Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [AllowedKeys(Б, П, С)]
  public class PtCommandModel : BaseCommandModel, IHasTime, Ask.Core.Shared.Interfaces.ExecutionInterfaces.IHasUnparsedParameters
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.PT).DisplayName;

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? TimeSource { get; set; }
    public double? Time { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public Dictionary<SwitchingBus, List<PointModel>> BusPointsDictionary { get; set; } = new();

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }
  }
}

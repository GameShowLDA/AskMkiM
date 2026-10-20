using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using static Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [AllowedKeys(ЗС, ЗР, П, С,Г, Т1)]
  public class PeCommandModel : BaseCommandModel, IError, IHasScheme, IHasUnparsedParameters
  {
    /// <inheritdoc />
    public override string Mnemonic => EnumExtensions.GetCommandDisplayInfo(MeasurementTypeCommand.PE).DisplayName;

    /// <summary>
    /// Тип измерительной команды.
    /// Определяет категорию и логику выполнения команды и используется для выбора соответствующего алгоритма измерения.
    /// </summary>
    public override MeasurementTypeCommand TypeCommand => MeasurementTypeCommand.PE;

    public SchemeModel Scheme { get; set; }
    public string? UnparsedParameters { get; set; }
  }
}

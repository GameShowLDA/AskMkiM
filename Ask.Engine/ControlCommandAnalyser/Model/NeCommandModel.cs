using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Т,
    //Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Б,
    Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Н)]
  public class NeCommandModel : BaseCommandModel, IHasScheme
  {
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.NE).DisplayName;

    /// <summary>
    /// Единицы измерения напряжения 
    /// </summary>
    public string? VoltageUnit { get; set; }

    /// <summary>
    /// Нижняя граница значеня напряжения при протекании тока через диод в прямом направлении
    /// </summary>
    public string? LowerLimitVoltageSource { get; set; }

    /// <summary>
    /// Нижняя граница значеня напряжения при протекании тока через диод в прямом направлении
    /// </summary>
    public double? LowerLimitVoltage { get; set; }

    /// <summary>
    /// Верхняя граница значеня напряжения при протекании тока через диод в прямом направлении
    /// </summary>
    public string? HigherLimitVoltageSource { get; set; }

    /// <summary>
    /// Верхняя граница значеня напряжения при протекании тока через диод в прямом направлении
    /// </summary>
    public double? HigherLimitVoltage { get; set; }
    /// <summary>
    /// Значение напряжения (например, "100В", "1кВ").
    /// </summary>
    public string? VoltageSource { get; set; }

    public double? Voltage { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public SchemeModel Scheme { get; set; }

    /// <summary>
    /// Тип подключения элементов (прямое/обратное) для первой точки измерения из цепи.
    /// </summary>
    public List<(ChainModel, ElementEnabling.Type)> ElementEnablingType = new();

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }
  }
}

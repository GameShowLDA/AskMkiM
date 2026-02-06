using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Г, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.К, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Т1)]
  [MeasurementDevice(MeasurementDevice.BreakdownTester)]
  public class PiCommandModel : BaseCommandModel, IHasScheme
  {
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PI).DisplayName;

    /// <summary>
    /// Модль команды СИ.
    /// </summary>
    public SiCommandModel SiCommand { get; set; }

    /// <summary>
    /// Значение напряжения (например, "100В", "1кВ").
    /// </summary>
    public string? VoltageSource { get; set; }

    public double? Voltage { get; set; }

    /// <summary>
    /// Тип напряжения.
    /// </summary>
    public VoltageEnum.Type VoltageType { get; set; }

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? TimeSource { get; set; }
    public double? Time { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public SchemeModel Scheme { get; set; }

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }

    public override IPointError PointErrors => new PiErrors();
  }
}

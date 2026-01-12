using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;

namespace Ask.Engine.ControlCommandAnalyser.Model
{

  /// <summary>
  /// Модель для команды СИ (сопротивление изоляции).
  /// </summary>
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.К,
    Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.С, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.П,
     Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Т,
      Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.И,
    Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Г, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Т1)]
  [MeasurementDevice(MeasurementDevice.BreakdownTester)]
  public class SiCommandModel : BaseCommandModel, IHasScheme
  {
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI).DisplayName;

    /// <summary>
    /// Значение напряжения (например, "100В", "1кВ").
    /// </summary>
    public string? VoltageSource { get; set; }
    public double? Voltage { get; set; }

    /// <summary>
    /// Единицы измерения сопротивления (например, "МОм", "кОм" и т.п.)
    /// </summary>
    public string? ResistanceUnit { get; set; }

    /// <summary>
    /// Значение сопротивления (например, "100<МОм").
    /// </summary>
    public string? ResistanceSource { get; set; }
    public double? Resistance { get; set; }

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

    public override IPointError PointErrors => new SiErrors();
  }
}

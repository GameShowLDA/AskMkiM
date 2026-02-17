using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using static Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель для команды СИ (сопротивление изоляции).
  /// </summary>
  [AllowedKeys(К,С,П,Т,И,Г,Т1)]
  [MeasurementDevice(MeasurementDevice.BreakdownTester)]
  public class SiCommandModel : BaseCommandModel, IHasScheme, ITimeCommandModel
  {
    /// <summary>
    /// Мнемоническое обозначение измерительной команды.
    /// Используется для отображения команды в пользовательском интерфейсе,
    /// протоколах выполнения и диагностических сообщениях.
    /// </summary>
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI).DisplayName;

    /// <summary>
    /// Тип измерительной команды.
    /// Определяет категорию и логику выполнения команды и используется для выбора соответствующего алгоритма измерения.
    /// </summary>
    public override MeasurementTypeCommand TypeCommand => MeasurementTypeCommand.SI;

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

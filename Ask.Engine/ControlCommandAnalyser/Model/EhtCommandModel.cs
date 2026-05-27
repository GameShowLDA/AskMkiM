using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using static Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [MeasurementDevice(MeasurementDevice.Multimeter)]
  [AllowedKeys(Д)]
  public class EhtCommandModel : BaseCommandModel, IHasScheme, ITimeCommandModel
  {
    /// <summary>
    /// Мнемоническое обозначение измерительной команды.
    /// Используется для отображения команды в пользовательском интерфейсе,
    /// протоколах выполнения и диагностических сообщениях.
    /// </summary>
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.EHT).DisplayName;

    /// <summary>
    /// Тип измерительной команды.
    /// Определяет категорию и логику выполнения команды и используется для выбора соответствующего алгоритма измерения.
    /// </summary>
    public override MeasurementTypeCommand TypeCommand => MeasurementTypeCommand.EHT;

    /// <summary>
    /// Единицы измерения сопротивления проводов (например, "МОм", "кОм" и т.п.)
    /// </summary>
    public string? CabelResistanceUnit { get; set; }

    /// <summary>
    /// Строка со значением сопротивления проводов (например, "100МОм")
    /// </summary>
    public string? CabelResistanceSource { get; set; }

    /// <summary>
    /// Значение сопротивления проводов (например, "100МОм")
    /// </summary>
    public double? CabelResistance { get; set; }

    /// <summary>
    /// Единицы измерения сопротивления (например, "МОм", "кОм" и т.п.)
    /// </summary>
    public string? ResistanceUnit { get; set; }

    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public string? LowerLimitResistanceSource { get; set; }

    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public double? LowerLimitResistance { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public string? HigherLimitResistanceSource { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public double? HigherLimitResistance { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public SchemeModel Scheme { get; set; }

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }

    /// <summary>
    /// Строка со значением силы тока.
    /// </summary>
    public string? AmperageSource { get; set; }

    /// <summary>
    /// Значение силы тока.
    /// </summary>
    public double? Amperage { get; set; }

    /// <summary>
    /// Единицы измерения силы тока.
    /// </summary>
    public string? AmperageUnit { get; set; }

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? TimeSource { get; set; }
    public double? Time { get; set; }

    /// <summary>
    /// Ошибки связанные с замыканием точек.
    /// </summary>
    public override IPointError PointErrors => new PrErrors();
  }
}

using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model.Pr
{
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.К,
   Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.ЗР,
   Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.ЗС,
   Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.С, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.П,
    Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.И,
    Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Т,
   Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Г, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Т1)]
  [MeasurementDevice(MeasurementDevice.Multimeter)]
  [ResistanceRange(1.0, 100000.0, 10.0)]
  public class PrCommandModel : BaseCommandModel, IError, IHasScheme
  {
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PR).DisplayName;

    /// <summary>
    /// Единицы измерения сопротивления (например, "МОм", "кОм" и т.п.)
    /// </summary>
    public string? ResistanceUnit { get; set; }

    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public string? DisconnectedLowerLimitResistanceSource { get; set; }

    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public double? DisconnectedLowerLimitResistance { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public string? DisconnectedHigherLimitResistanceSource { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public double? DisconnectedHigherLimitResistance { get; set; }

    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public string? ConnectedLowerLimitResistanceSource { get; set; }

    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public double? ConnectedLowerLimitResistance { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public string? ConnectedHigherLimitResistanceSource { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public double? ConnectedHigherLimitResistance { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public SchemeModel Scheme { get; set; }

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? TimeSource { get; set; }
    public double? Time { get; set; }

    /// <summary>
    /// Ошибки связанные с замыканием точек.
    /// </summary>
    public override IPointError PointErrors => new PrErrors();

    /// <summary>
    /// Сбор данных в сообщение.
    /// </summary>
    public override IDislpayInfo BuildDislpayInfo => new PrMessageBuild();
  }
}

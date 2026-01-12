using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model.Ie
{
  /// <summary>
  /// Модель для команды ИЕ (измерение емкости).
  /// </summary>
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
  [MeasurementDevice(MeasurementDevice.Multimeter)]
  public class IeCommandModel : BaseCommandModel, IHasScheme
  {

    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.IE).DisplayName;

    /// <summary>
    /// Единицы измерения электрической ёмкости (например, "МОм", "кОм" и т.п.)
    /// </summary>
    public string? CapacityUnit { get; set; }

    /// <summary>
    /// Нижняя граница значеня элктрической ёмкости в строке (например, "100<МОм")
    /// </summary>
    public string? LowerLimitCapacitySource { get; set; }

    /// <summary>
    /// Нижняя граница значеня элктрической ёмкости в плавающей запятой.
    /// </summary>
    public double? LowerLimitCapacity { get; set; }

    /// <summary>
    /// Верхняя граница элктрической ёмкости (например, "МОм<100").
    /// </summary>
    public string? HigherLimitCapacitySource { get; set; }

    /// <summary>
    /// Верхняя граница значеня элктрической ёмкости в плавающей запятой.
    /// </summary>
    public double? HigherLimitCapacity { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public SchemeModel Scheme { get; set; }

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }

    /// <summary>
    /// Ошибки связанные с замыканием точек.
    /// </summary>
    public override IPointError PointErrors => new IeErrors();

    /// <summary>
    /// Сбор данных в сообщение.
    /// </summary>
    public override IDislpayInfo BuildDislpayInfo => new IeMessageBuild();
  }
}

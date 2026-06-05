using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Т,
    //Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Б,
    Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Н)]
  [MeasurementDevice(MeasurementDevice.Multimeter)]
  public class NeCommandModel : BaseCommandModel, IError, IHasScheme, IHasAmperage, IHasVoltage, IHasVoltageLimits, IHasUnparsedParameters
  {
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.NE).DisplayName;

    public override MeasurementTypeCommand TypeCommand => MeasurementTypeCommand.NE;

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

    /// <summary>
    /// Ошибки связанные с замыканием точек.
    /// </summary>
    public override IPointError PointErrors => new NeErrors();

    /// <summary>
    /// Сбор данных в сообщение.
    /// </summary>
    public override IDislpayInfo BuildDislpayInfo => new NeMessageBuild();

    public double? Amperage { get; set; }
    public string? AmperageSource { get; set; }
    public bool HasAmperage { get; set; } = false;
    public string? AmperageUnit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}

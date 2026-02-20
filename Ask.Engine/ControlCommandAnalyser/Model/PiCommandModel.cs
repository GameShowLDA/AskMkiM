using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using static Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [AllowedKeys(Г, К, Т1)]
  [MeasurementDevice(MeasurementDevice.BreakdownTester)]
  public class PiCommandModel : BaseCommandModel, IHasScheme
  {
    /// <summary>
    /// Мнемоническое обозначение измерительной команды.
    /// Используется для отображения команды в пользовательском интерфейсе,
    /// протоколах выполнения и диагностических сообщениях.
    /// </summary>
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PI).DisplayName;

    /// <summary>
    /// Тип измерительной команды.
    /// Определяет категорию и логику выполнения команды и используется для выбора соответствующего алгоритма измерения.
    /// </summary>
    public override MeasurementTypeCommand TypeCommand => MeasurementTypeCommand.PI;

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

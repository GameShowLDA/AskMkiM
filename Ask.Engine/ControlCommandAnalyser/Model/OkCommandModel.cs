using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель команды ОК (объект контроля).
  /// </summary>
  public class OkCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.OK).DisplayName;

    /// <summary>
    /// Обозначение объекта контроля (обязательно, до 39 символов).
    /// </summary>
    public string ObjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Наименование объекта контроля (до 39 символов). Может отсутствовать.
    /// </summary>
    public string? ObjectName { get; set; }

    /// <summary>
    /// Обозначение объекта контроля.
    /// </summary>
    public string ControlObjectTitle { get; set; }

    /// <summary>
    /// Наименование объекта контроля.
    /// </summary>
    public string? ControlObjectName { get; set; }

    /// <summary>
    /// Обозначение бумажного документа таблицы программы испытаний при его наличии (ОПК).
    /// </summary>
    public string? TestProgramTableDesignation { get; set; }

    /// <summary>
    /// Номер последнего проведенного измерения в ОПК.
    /// </summary>
    public string? LastMeasurment { get; set; }

    /// <summary>
    /// Тип системы контроля.
    /// </summary>
    public string ContolSystemType { get; set; }

    /// <summary>
    /// Обозначение документа, на основании которого написана программа контроля (КД).
    /// </summary>
    public List<string?>? ControlProgramDocument { get; set; }

    /// <summary>
    /// Номер последнего проведенного в КД извещения.
    /// </summary>
    public List<Tuple<string, string?>?>? LastNotificationNumber { get; set; }

    /// <summary>
    /// Номер заказа.
    /// </summary>
    public string? OrderNumber { get; set; }

    /// <summary>
    /// Номер потребителя программы (ЦЕХ).
    /// </summary>
    public string? DepartmentNumber { get; set; }

    /// <summary>
    /// Примечания.
    /// </summary>
    public List<string>? Comments { get; set; } = new();

    public ProtocolModel ProtocolModel { get; set; } = new();
  }
}

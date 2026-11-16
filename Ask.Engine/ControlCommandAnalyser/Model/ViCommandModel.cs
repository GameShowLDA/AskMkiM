using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using static Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey;

namespace Ask.Engine.ControlCommandAnalyser.Model;

/// <summary>
/// Содержит результат трансляции команды управления источником питания.
/// </summary>
[AllowedKeys(Б)]
public sealed class ViCommandModel : BaseCommandModel
{
  /// <inheritdoc />
  public override string Mnemonic => EnumExtensions.GetCommandOrganizationalInfo(OrganizationalComands.VI).DisplayName;

  /// <summary>
  /// Номер источника питания.
  /// </summary>
  public int? PowerSupplyNumber { get; set; }

  /// <summary>
  /// Номер тестера, к которому относится найденный источник.
  /// </summary>
  public int? PowerSupplyChassisNumber { get; set; }

  /// <summary>
  /// Заданное напряжение в вольтах.
  /// </summary>
  public decimal? Voltage { get; set; }

  /// <summary>
  /// Исходная запись напряжения.
  /// </summary>
  public string? VoltageSource { get; set; }

  /// <summary>
  /// Заданное ограничение тока в амперах.
  /// </summary>
  public decimal? Current { get; set; }

  /// <summary>
  /// Исходная запись ограничения тока.
  /// </summary>
  public string? CurrentSource { get; set; }

  /// <summary>
  /// Шина, подключаемая к положительному выводу.
  /// </summary>
  public SwitchingBus? PositiveBus { get; set; }

  /// <summary>
  /// Шина, подключаемая к отрицательному выводу.
  /// </summary>
  public SwitchingBus? NegativeBus { get; set; }

  /// <summary>
  /// Действие, определённое сочетанием параметров команды.
  /// </summary>
  public ViOperation Operation { get; set; }
}

/// <summary>
/// Задаёт действие команды ВИ.
/// </summary>
public enum ViOperation
{
  /// <summary>
  /// Действие не определено из-за ошибок трансляции.
  /// </summary>
  Unknown,

  /// <summary>
  /// Отключить источник от шин и освободить его.
  /// </summary>
  DisconnectAndRelease,

  /// <summary>
  /// Задать напряжение и ток без подключения к шинам.
  /// </summary>
  ConfigureWithoutBuses,

  /// <summary>
  /// Задать напряжение и ток, затем подключить источник к шинам.
  /// </summary>
  ConfigureAndConnect
}

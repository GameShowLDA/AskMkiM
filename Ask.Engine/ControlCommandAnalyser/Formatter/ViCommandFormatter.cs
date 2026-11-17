using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter;

/// <summary>
/// Формирует текст трансляции команды ВИ.
/// </summary>
internal sealed class ViCommandFormatter : CommandFormatter<ViCommandModel>
{
  protected override IEnumerable<string> Format(ViCommandModel model)
  {
    foreach (var line in FormatCommandStart(model))
      yield return line;

    if (model.PowerSupplyNumber.HasValue)
    {
      string chassis = model.PowerSupplyChassisNumber.HasValue
        ? $" (тестер {model.PowerSupplyChassisNumber})"
        : string.Empty;
      yield return $"\tИсточник питания: №{model.PowerSupplyNumber}{chassis}";
    }

    yield return model.Operation switch
    {
      ViOperation.DisconnectAndRelease => "\tДействие: отключить от шин и освободить источник",
      ViOperation.ConfigureWithoutBuses => "\tДействие: задать параметры без подключения к шинам",
      ViOperation.ConfigureAndConnect => "\tДействие: задать параметры и подключить к шинам",
      _ => "\tДействие не определено"
    };

    if (!string.IsNullOrWhiteSpace(model.VoltageSource))
      yield return $"\tНапряжение: {model.VoltageSource}";

    if (!string.IsNullOrWhiteSpace(model.CurrentSource))
      yield return $"\tТок: {model.CurrentSource}";

    if (model.PositiveBus.HasValue)
      yield return $"\tПоложительная шина: +{model.PositiveBus}";

    if (model.NegativeBus.HasValue)
      yield return $"\tОтрицательная шина: -{model.NegativeBus}";

    foreach (var line in FormatComments(model))
      yield return line;

    foreach (var line in FormatEnd())
      yield return line;
  }
}

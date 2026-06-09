using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class NeCommandFormatter : CommandFormatter<NeCommandModel>
  {
    protected override IEnumerable<string> Format(NeCommandModel ne)
    {
      foreach (var line in FormatCommandStart(ne))
      {
        yield return line;
      }

      foreach (var line in FormatMeter())
      {
        yield return line;
      }

      foreach (var line in FormatComments(ne))
      {
        yield return line;
      }

      if (!HasRmModel())
      {
        yield return "\tМодель РМ не задана!";
        yield break;
      }

      if (ne.Scheme == null || ne.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      yield return "\tПроверка диода в прямом направлении:";
      yield return VoltageFormatter.FormatVoltageLowerLimit(ne, "\t\t");
      yield return VoltageFormatter.FormatVoltageHigherLimit(ne, "\t\t");

      if (!ne.AlgorithmKey.Contains(AlgorithmKey.Н.ToString()))
      {
        yield return "\tПроверка диода в обратном направлении:";
        yield return VoltageFormatter.FormatVoltage(ne);
      }

      if (ne.Scheme.GroupModels.Count > 0)
      {
        yield return "\t\tСписок проверяемых точек:";

        foreach (var line in SchemeFormatter.FormatConnectedChains(ne.Scheme, index => FormatElementPrefix(ne, index)))
        {
          yield return line;
        }
      }

      foreach (var line in FormatEnd())
      {
        yield return line;
      }
    }

    private static IEnumerable<string> FormatMeter()
    {
      var meter = FastMeters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
      if (meter == null)
      {
        LogError("Не найден измеритель.");
        yield break;
      }

      yield return $"\tИспользуемый измеритель: {meter.Name}";
    }

    private static string FormatElementPrefix(NeCommandModel ne, int index)
    {
      return index < ne.ElementEnablingType.Count
        ? ne.ElementEnablingType[index].Item2.GetDescription()
        : string.Empty;
    }
  }
}

using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  internal class SchemeFormatter
  {
    public static IEnumerable<string> FormatSchemePoints(
      IHasScheme model,
      string indent = "\t\t")
    {
      if (model.Scheme == null || model.Scheme.IsEmpty())
      {
        yield return $"{indent}Точки не заданы!";
        yield break;
      }

      for (int i = 0; i < model.Scheme.GroupModels.Count; i++)
      {
        var connectedPoints = model.Scheme.GetPointsConnected(model.Scheme.GroupModels[i]);
        if (connectedPoints == null)
        {
          continue;
        }

        foreach (var chain in connectedPoints.ChainModels)
        {
          yield return FormatChain(chain, i + 1, indent);
        }
      }
    }

    public static string FormatChain(
      ChainModel chain,
      int index,
      string indent = "\t\t")
    {
      var points = string.Join(
        ",",
        chain.PointModels.Select(point => $"{point.Mnemonic}[{point}]"));

      return $"{indent}{index}. *{points}";
    }

    public static IEnumerable<string> FormatBusPoints(
      List<SwitchingBus>? buses,
      string title = "Сбрасываемые точки с шин",
      string indent = "\t")
    {
      if (buses == null || !buses.Any())
      {
        yield return $"{indent}{title} не заданы!";
        yield break;
      }

      yield return $"{indent}{title}:";

      foreach (var bus in buses)
      {
        yield return $"{indent}\t{bus}";
      }
    }

    public static IEnumerable<string> FormatConnectedChains(
      SchemeModel scheme,
      string indent = "\t\t\t")
    {
      return FormatConnectedChains(scheme, _ => string.Empty, indent);
    }

    public static IEnumerable<string> FormatConnectedChains(
      SchemeModel scheme,
      Func<int, string> pointPrefixFactory,
      string indent = "\t\t\t")
    {
      var index = 1;

      for (int i = 0; i < scheme.GroupModels.Count; i++)
      {
        var groupChains = scheme.GetPointsConnected(scheme.GroupModels[i]);
        if (groupChains == null)
        {
          continue;
        }

        foreach (var chain in groupChains.ChainModels)
        {
          var pointPrefix = pointPrefixFactory(index - 1);
          yield return $"{indent}{index}. *{pointPrefix}{FormatPointList(chain.PointModels, ",")}";
          index++;
        }
      }
    }

    public static IEnumerable<string> FormatDisconnectedPoints(
      SchemeModel scheme,
      string indent = "\t\t\t")
    {
      for (int i = 0; i < scheme.GroupModels.Count; i++)
      {
        var points = scheme.GetPointsDisconnected(scheme.GroupModels[i]);
        if (points == null)
        {
          continue;
        }

        yield return $"{indent}{i + 1}. *{FormatPointList(points.PointModels, "#")}";
      }
    }

    public static string FormatPointList(
      IEnumerable<PointModel> points,
      string separator)
    {
      return string.Join(
        separator,
        points.Select(point => $"{point.Mnemonic}[{point}]"));
    }

    public static IEnumerable<string> FormatCommutationRackStructure(IHasRackStructure model)
    {
      if (model.BusStructure.Count == 0)
      {
        yield return "\tСтруктура стойки коммутации не указана!";
      }
      else
      {
        yield return "\tСтруктура стойки коммутации:";
      }

      foreach (var item in model.BusStructure)
      {
        if (item.Value.Count == 0)
        {
          continue;
        }

        var text = item.Key switch
        {
          BusStructureEnum.Type.Bus2 => "\t\tДвухшинное подключение",
          BusStructureEnum.Type.Bus4 => "\t\tЧетырехшинное подключение",
          BusStructureEnum.Type.Bus6 => "\t\tШестишинное подключение",
          BusStructureEnum.Type.Bus8 => "\t\tВосьмишинное подключение",
          BusStructureEnum.Type.BusCombined => "\t\tКомбинированное подключение",
          _ => throw new ArgumentOutOfRangeException(nameof(item.Key))
        };

        yield return $"{text}: {string.Join(", ", item.Value)}";
      }
    }
  }
}

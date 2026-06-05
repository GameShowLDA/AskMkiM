using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  public abstract class CommandFormatter<TCommandModel> : ICommandFormatter
    where TCommandModel : BaseCommandModel
  {
    public bool CanFormat(BaseCommandModel model) => model is TCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      return model is TCommandModel commandModel
        ? Format(commandModel)
        : Enumerable.Empty<string>();
    }

    protected abstract IEnumerable<string> Format(TCommandModel model);

    protected static IEnumerable<string> FormatCommandStart(
      BaseCommandModel model,
      string? header = null,
      bool includeKey = true)
    {
      yield return header ?? FormatMnemonic(model);

      if (model is IHasUnparsedParameters unparsedModel
        && !string.IsNullOrWhiteSpace(unparsedModel.UnparsedParameters))
      {
        yield return $"\t{unparsedModel.UnparsedParameters}";
      }

      if (includeKey && model.AlgorithmKey.Count > 0)
      {
        yield return FormatKeys(model);
      }
    }

    protected static string FormatKeys(BaseCommandModel model)
    {
      return model.AlgorithmKey.Count > 0
        ? $"\tКлючи команды: {string.Join(", ", model.AlgorithmKey)}"
        : string.Empty;
    }

    protected static IEnumerable<string> FormatComments(BaseCommandModel model)
    {
      if (model.Comment.Count == 0)
      {
        yield break;
      }

      yield return "\tКомментарии:";

      foreach (var line in model.Comment)
      {
        var trimmed = line.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
          yield return $"\t\t{trimmed}";
        }
      }
    }

    protected static IEnumerable<string> FormatEnd()
    {
      yield return string.Empty;
    }

    protected static IEnumerable<string> FormatSchemeWithRmCheck(
      IHasScheme model,
      string title,
      string rmNotSetMessage = "\t\tМодель РМ не задана!")
    {
      yield return title;

      if (!HasRmModel())
      {
        yield return rmNotSetMessage;
        yield break;
      }

      foreach (var line in SchemeFormatter.FormatSchemePoints(model))
      {
        yield return line;
      }
    }

    protected static bool HasRmModel()
    {
      return CommandsModel.GetRMModel() != null;
    }

    protected static IEnumerable<string> FormatBusPointGroups(
      Dictionary<SwitchingBus, List<PointModel>> busPoints,
      string title)
    {
      if (busPoints.Count == 0)
      {
        yield return $"{title} не заданы!";
        yield break;
      }

      foreach (var bus in busPoints)
      {
        yield return $"{title}: {bus.Key}";

        foreach (var point in bus.Value)
        {
          yield return $"\t\t{point.Mnemonic} = {point}";
        }
      }
    }

    private static string FormatMnemonic(BaseCommandModel model)
    {
      return $"{model.CommandNumber} {model.Mnemonic}";
    }
  }
}

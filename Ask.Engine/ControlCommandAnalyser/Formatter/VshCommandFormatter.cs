using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class VshCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is VshCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not VshCommandModel vsh)
        yield break;

      var firstLine = $"{vsh.CommandNumber} {vsh.Mnemonic}";
      yield return firstLine;

      // Ключи
      if (vsh.AlgorithmKey.Count > 0)
        yield return $"\tКлючи команды: {string.Join(", ", vsh.AlgorithmKey)}";

      if (vsh.BusStructure.Count == 0)
      {
        yield return $"\tСтруктура стойки коммутации не указана!";
      }
      else
      {
        yield return $"\tСтруктура стойки коммутации:";
      }

      foreach (var item in vsh.BusStructure)
      {
        if (item.Value.Count > 0)
        {
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

      if (vsh.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in vsh.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

      yield return string.Empty;
    }
  }
}

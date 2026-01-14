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

      if(vsh.BusStructure.Count == 0)
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
          if (item.Key == BusStructureEnum.Type.Bus2)
          {
            yield return $"\t\tДвухшинное подключение: {string.Join(", ", item.Value)}";
          }
          if (item.Key == BusStructureEnum.Type.Bus4)
          {
            yield return $"\t\tЧетырехшинное подключение: {string.Join(", ", item.Value)}";
          }
          if (item.Key == BusStructureEnum.Type.Bus6)
          {
            yield return $"\t\tШестишинное подключение: {string.Join(", ", item.Value)}";
          }
          if (item.Key == BusStructureEnum.Type.Bus8)
          {
            yield return $"\t\tВосьмишинное подключение: {string.Join(", ", item.Value)}";
          }
          if (item.Key == BusStructureEnum.Type.BusCombined)
          {
            yield return $"\t\tКомбинированное подключение: {string.Join(", ", item.Value)}";
          }
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

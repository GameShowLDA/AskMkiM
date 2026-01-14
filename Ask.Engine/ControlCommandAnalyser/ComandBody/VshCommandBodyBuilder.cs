using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class VshCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is VshCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not VshCommandModel vsh)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (vsh.BusStructure.Count > 0)
      {
        var busNumber = string.Empty;
        commandBody.Append($"*");
        foreach (var item in vsh.BusStructure)
        {
          if(item.Key == BusStructureEnum.Type.Bus2)
          {
            busNumber = "2";
          }
          if(item.Key == BusStructureEnum.Type.Bus4)
          {
            busNumber = "4";
          }
          if(item.Key == BusStructureEnum.Type.Bus6)
          {
            busNumber = "6";
          }
          if(item.Key == BusStructureEnum.Type.Bus8)
          {
            busNumber = "8";
          }
          if(item.Key == BusStructureEnum.Type.BusCombined)
          {
            busNumber = "К";
          }
          foreach (var number in item.Value)
          {
            commandBody.Append($"{busNumber}Ш:{number}*");
          }
        }
      }
      return newSourseLines.Append(commandBody.ToString());
    }
  }
}

using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;
using System.Text;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class PiCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is PiCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not PiCommandModel pi)
      {
        return newSourseLines;
      }
      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(newSourseLines.ToString(), model);
      var strSourseLine = newSourseLines.ToString();
      foreach (var key in model.AlgorithmKey)
      {
        strSourseLine = Regex.Replace(
        strSourseLine,
        $@"\b{Regex.Escape(key)}\s*,?",
        "",
        RegexOptions.IgnoreCase);
      }
      newSourseLines.Clear();
      newSourseLines.Append(strSourseLine);
      var commandBody = new StringBuilder();
      var siCommand = string.Empty;
      if (pi.SiCommand != null)
      {
        var siBuilder = new SiCommandBodyBuilder();
        commandBody = siBuilder.Create(pi.SiCommand, commandBody);

        var algorithmKey = pi.SiCommand.AlgorithmKey;
        if (algorithmKey != null)
        {
          var algorithmKeysList = algorithmKey.ToList();

          for (int i = 0; i < algorithmKeysList.Count; i++)
          {
            if (!string.IsNullOrEmpty(algorithmKeysList[i]) && !string.IsNullOrWhiteSpace(algorithmKeysList[i]) && i < algorithmKeysList.Count - 1)
            {
              commandBody.Append($"{algorithmKeysList[i]}, ");
            }
            else
            {
              commandBody.Append($"{algorithmKeysList[i]} ");
            }
          }
        }

      }
      if (pi.VoltageType == VoltageEnum.Type.DCW)
      {
        commandBody.Append('+');
      }
      if (!string.IsNullOrEmpty(pi.VoltageSource))
      {
        commandBody.Append($"{pi.VoltageSource}");
      }
      if (!string.IsNullOrEmpty(pi.TimeSource))
      {
        commandBody.Append($", {pi.TimeSource}");
      }
      var piAlgorithmKey = pi.AlgorithmKey;
      if (piAlgorithmKey != null)
      {
        var piAlgorithmKeysList = piAlgorithmKey.ToList();
        commandBody.Append($", ");
        for (int i = 0; i < piAlgorithmKeysList.Count; i++)
        {
          if (!string.IsNullOrEmpty(piAlgorithmKeysList[i]) && !string.IsNullOrWhiteSpace(piAlgorithmKeysList[i]) && i < piAlgorithmKeysList.Count - 1)
          {
            commandBody.Append($"{piAlgorithmKeysList[i]}, ");
          }
          else
          {
            commandBody.Append($"{piAlgorithmKeysList[i]} ");
          }
        }
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}

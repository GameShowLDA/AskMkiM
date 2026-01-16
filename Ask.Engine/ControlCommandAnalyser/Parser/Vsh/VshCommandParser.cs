using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;
using DataBaseConfiguration.Services.Device;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Vsh
{
  public class VshCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.VSH);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new VshCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);

      // Убираем полностью пустые/пробельные строки (чтобы не таскать мусор)
      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
      }

      ParseVshCommand(string.Join(Environment.NewLine, processedLines), model);

      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      return model;
    }

    public static VshCommandModel ParseVshCommand(string input, VshCommandModel model)
    {
      var regex = new Regex(
          @"\*?(?<prefix>[2468К])Ш(?::(?<value>\d+))?\*|\b(?<prefix>[2468К])Ш\b",
          RegexOptions.IgnoreCase
      );

      var matches = regex.Matches(input);
      var busDictionary = new Dictionary<BusStructureEnum.Type, List<int?>>();

      foreach (Match match in matches)
      {
        var prefix = match.Groups["prefix"].Value;

        int? value = match.Groups["value"].Success
            ? int.Parse(match.Groups["value"].Value)
            : null;
        if (value == null)
        {
          //var breakDown = ServiceLocator.GetRequired<IBreakdownTester>();
          var managerShassi = new ChassisManagerServices().GetAllEntities().FirstOrDefault();
          if (managerShassi != null)
          {
            busDictionary = ManageBusStructure(model, prefix, managerShassi.Number, busDictionary);
          }
          var managerRack = new RackServices().GetAllEntities();
          if (managerRack != null && managerRack.Count > 0)
          {
            foreach (var rack in managerRack)
            {
              busDictionary = ManageBusStructure(model, prefix, rack.Number, busDictionary);
            }
          }
          continue;
        }

        busDictionary = ManageBusStructure(model, prefix, value, busDictionary);
      }
      model.BusStructure = busDictionary;

      return model;
    }

    private static Dictionary<BusStructureEnum.Type, List<int?>> ManageBusStructure(VshCommandModel model, string prefix, int? standNumber, Dictionary<BusStructureEnum.Type, List<int?>> busDictionary)
    {
      if (!string.IsNullOrWhiteSpace(prefix))
      {
        var standList = new List<int?>();
        if (standNumber.HasValue)
        {
          standList.Add(standNumber.Value);
        }
        if (prefix == "2")
        {
          if (!busDictionary.ContainsKey(BusStructureEnum.Type.Bus2))
          {
            busDictionary.Add(BusStructureEnum.Type.Bus2, standList);
          }
          else
          {
            busDictionary[BusStructureEnum.Type.Bus2].Add(standNumber.Value);
          }
        }
        else if (prefix == "4")
        {
          if (!busDictionary.ContainsKey(BusStructureEnum.Type.Bus4))
          {
            busDictionary.Add(BusStructureEnum.Type.Bus4, standList);
          }
          else
          {
            busDictionary[BusStructureEnum.Type.Bus4].Add(standNumber.Value);
          }
        }
        else if (prefix == "6")
        {
          if (!busDictionary.ContainsKey(BusStructureEnum.Type.Bus6))
          {
            busDictionary.Add(BusStructureEnum.Type.Bus6, standList);
          }
          else
          {
            busDictionary[BusStructureEnum.Type.Bus6].Add(standNumber.Value);
          }
        }
        else if (prefix == "8")
        {
          if (!busDictionary.ContainsKey(BusStructureEnum.Type.Bus8))
          {
            busDictionary.Add(BusStructureEnum.Type.Bus8, standList);
          }
          else
          {
            busDictionary[BusStructureEnum.Type.Bus8].Add(standNumber.Value);
          }
        }
        else if (prefix == "К")
        {
          if (!busDictionary.ContainsKey(BusStructureEnum.Type.BusCombined))
          {
            busDictionary.Add(BusStructureEnum.Type.BusCombined, standList);
          }
          else
          {
            busDictionary[BusStructureEnum.Type.BusCombined].Add(standNumber.Value);
          }
        }
        else
        {
          model.Errors.Add(VshErrors.InvalidVshBusStructure(model.StartLineNumber, model.Mnemonic));
        }
        return busDictionary;
      }
      return busDictionary;
    }
  }
}

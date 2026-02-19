using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using DataBaseConfiguration.Services.Device;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
{
  internal class BusStructureParser
  {
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
        var shassiBusType = BusStructureEnum.Type.None;
        var managerShassi = new ChassisManagerServices().GetAllEntities().FirstOrDefault();
        if (managerShassi != null)
        {
          shassiBusType = managerShassi.BusType;
        }
        if (value == null)
        {
          if (managerShassi != null)
          {
            busDictionary = ManageBusStructure(model, prefix, managerShassi.Number, busDictionary, shassiBusType);
          }
          var managerRack = new RackServices().GetAllEntities();
          if (managerRack != null && managerRack.Count > 0)
          {
            foreach (var rack in managerRack)
            {
              busDictionary = ManageBusStructure(model, prefix, rack.Number, busDictionary, shassiBusType);
            }
          }
          continue;
        }

        busDictionary = ManageBusStructure(model, prefix, value, busDictionary, shassiBusType);
      }
      model.BusStructure = busDictionary;

      return model;
    }

    private static Dictionary<BusStructureEnum.Type, List<int?>> ManageBusStructure(VshCommandModel model, string prefix, int? standNumber,
      Dictionary<BusStructureEnum.Type, List<int?>> busDictionary, BusStructureEnum.Type shassiBusType)
    {
      if (string.IsNullOrWhiteSpace(prefix))
        return busDictionary;

      var prefixMap = new Dictionary<string, BusStructureEnum.Type>
      {
        ["2"] = BusStructureEnum.Type.Bus2,
        ["4"] = BusStructureEnum.Type.Bus4,
        ["6"] = BusStructureEnum.Type.Bus6,
        ["8"] = BusStructureEnum.Type.Bus8,
        ["К"] = BusStructureEnum.Type.BusCombined
      };

      int.TryParse(shassiBusType.GetDescription(), out int resultShassi);

      if (!prefixMap.TryGetValue(prefix, out var busType))
      {
        int.TryParse(busType.GetDescription(), out int resultBus);
        string shassiStr = CreateShassiString(resultShassi);
        model.Errors.Add(
            VshErrors.InvalidVshBusStructure(model.StartLineNumber, model.Mnemonic, shassiStr));
        return busDictionary;
      }
      if (shassiBusType == BusStructureEnum.Type.None)
      {
        model.Errors.Add(
              VshErrors.NoneVshBusStructure(model.StartLineNumber, model.Mnemonic));
        return busDictionary;
      }
      else
      {
        int.TryParse(busType.GetDescription(), out int resultBus);
        if (!busDictionary.TryGetValue(busType, out var standList))
        {
          standList = new List<int?>();
          if (resultBus <= resultShassi)
          {
            busDictionary[busType] = standList;
          }
          else
          {
            string shassiStr = CreateShassiString(resultShassi);
            model.Errors.Add(
              VshErrors.InvalidVshBusStructure(model.StartLineNumber, model.Mnemonic, shassiStr));
            return busDictionary;
          }
        }

        if (standNumber.HasValue)
        {
          standList.Add(standNumber.Value);
        }
      }

      return busDictionary;
    }

    private static string CreateShassiString(int resultShassi)
    {
      var shassiStr = string.Empty;
      for (int i = 2; i <= resultShassi; i += 2)
      {
        if (i == resultShassi)
        {
          shassiStr += i.ToString() + "Ш";
        }
        else
        {
          shassiStr += i.ToString() + "Ш, ";
        }
      }

      return shassiStr;
    }
  }
}

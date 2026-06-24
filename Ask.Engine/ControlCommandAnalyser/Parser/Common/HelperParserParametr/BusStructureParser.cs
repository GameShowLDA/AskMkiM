using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
{
  /// <summary>
  /// Вспомогательный парсер структуры шин (Ш) для команды ВШ.
  /// Извлекает из строки указания шин и формирует структуру
  /// <see cref="VshCommandModel.BusStructure"/> с учётом конфигурации стойки и шасси.
  /// </summary>
  internal class BusStructureParser
  {
    /// <summary>
    /// Разбирает строку команды ВШ и заполняет структуру шин модели.
    /// </summary>
    /// <param name="input">Исходная строка команды.</param>
    /// <param name="model">Модель команды ВШ.</param>
    /// <returns>Обновлённая модель с заполненной структурой шин.</returns>
    /// <remarks>
    /// Поддерживает указание конкретного номера (например, 4Ш:2)
    /// или применение ко всем доступным стойкам/шасси.
    /// </remarks>
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
        var managerShassi = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
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
          var managerRack = Racks.GetAllAsync().GetAwaiter().GetResult();
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

    /// <summary>
    /// Обрабатывает найденный префикс шины и добавляет номер стойки
    /// в словарь структуры шин с учётом ограничений конфигурации.
    /// </summary>
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
          var chassisManager = ChassisManagers.GetAllAsync().GetAwaiter().GetResult();

          bool rackExists = chassisManager.Any(c => c.Number == standNumber.Value);

          if (!rackExists)
          {
            model.Errors.Add(
                VshErrors.InvalidRackNumber(
                    model.StartLineNumber,
                    model.Mnemonic,
                    standNumber.Value));

            return busDictionary;
          }

          standList.Add(standNumber.Value);
        }
      }

      return busDictionary;
    }

    /// <summary>
    /// Формирует строковое представление доступных шин шасси
    /// для использования в сообщениях об ошибках.
    /// </summary>
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

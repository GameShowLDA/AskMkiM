using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  public class CommandsModel
  {
    public static List<BaseCommandModel> CommandModels = new();

    // TODO : Сделать рефлекией вытаскивание всех мнемоник команд, которые могут быть проверочными
    public static List<string> CheckCommandMnemonic = new List<string> { "ПР", "ПИ", "СИ", "ПЭ", "ЭТ" };

    /// <summary>
    /// Получает последнюю команду из списка команд проверки.
    /// </summary>
    /// <returns>Найденную команду или null.</returns>
    public static BaseCommandModel GetLastFromCheckCommands()
    {
      var commands = CommandModels.Where(comand => CheckCommandMnemonic.Contains(comand.Mnemonic)).ToList();
      if (commands.Count > 0)
      {
        return commands.Last();
      }
      else
      {
        return null;
      }
    }

    public static BaseCommandModel GetLast()
    {
      if (CommandModels.Count > 0)
      {
        return CommandModels[CommandModels.Count - 1];
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Получает модель команды по заданному номеру.
    /// </summary>
    /// <param name="comandNumber">Номер искомой команды.</param>
    /// <returns>Модель найденной команды или null.</returns>
    public static BaseCommandModel GetByComandNumber(string comandNumber)
    {
      return CommandModels.FirstOrDefault(x => x.CommandNumber == comandNumber);
    }

    /// <summary>
    /// Получает список моделей команд по указанной мнемонике.
    /// </summary>
    /// <param name="mnemonic">Мнемоника искомой команды.</param>
    /// <returns>Список найденных моделей команд.</returns>
    public static List<BaseCommandModel> GetByMnemonic(string mnemonic)
    {
      return CommandModels.Where(x => x.Mnemonic == mnemonic).ToList();
    }

    /// <summary>
    /// Получает модель команды РМ.
    /// </summary>
    /// <param name="mnemonic">Мнемоника искомой команды.</param>
    /// <returns>Модель найденной команды РМ или null, если команда не была найдена.</returns>
    public static RmCommandModel GetRMModel()
    {
      var foundCommands = GetByMnemonic("РМ");
      if (foundCommands != null && foundCommands.Count > 0)
      {
        var rmCommand = foundCommands.LastOrDefault();
        if (rmCommand != null && rmCommand is RmCommandModel result)
        {
          return result;
        }
        return null;
      }
      return null;
    }

    /// <summary>
    /// Статический метод для очистки списка моделей команд.
    /// </summary>
    public static void Clear()
    {
      CommandModels.Clear();
    }

    public static SchemeModel GetPointsFromPM(SchemeModel scheme)
    {
      // добавляем просто точки из РМ, которые еще не были записаны
      var rmCommand = GetRMModel();
      if (rmCommand != null)
      {
        List<PointModel> allPoints = new();
        if (scheme != null && scheme.GroupModels.Count > 0)
        {
          allPoints = GetAllPoints(scheme);
        }
        foreach (var pointDictionary in rmCommand.PointsMap)
        {
          var point = pointDictionary.Value;
          var mnemonic = pointDictionary.Key;
          var pointModel = PointModel.ParsePointString(point);
          pointModel.Mnemonic = mnemonic;
          pointModel.PointType = PointType.Star;
          var chainModel = new ChainModel(new List<PointModel>() { pointModel });
          var groupModel = new GroupModel(new List<ChainModel> { chainModel });
          if (scheme == null)
          {
            scheme = new SchemeModel(new List<GroupModel> { groupModel });
          }
          else
          {
            if (ComparePoints(scheme, allPoints, pointModel, groupModel) == false)
            {
              scheme.GroupModels.Add(groupModel);
            }
          }
          LogInformation(
            $"Схема распознана из РМ: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");
        }
      }
      else
      {
        LogWarning($"Команда РМ не найдена.");
      }
      return scheme;
    }

    private static bool ComparePoints(SchemeModel scheme, List<PointModel> allPoints, PointModel pointModel, GroupModel groupModel)
    {
      if (allPoints.Count > 0)
      {
        foreach (var pointElement in allPoints)
        {
          if (pointModel.ToString() == pointElement.ToString())
          {
            return true;
          }
        }
      }
      return false;
    }

    public static SchemeModel GetShemeFromLastCommand(BaseCommandModel model, SchemeModel scheme, BaseCommandModel lastCommand)
    {
      var foundCommandMnemonic = lastCommand.Mnemonic;
      var newCommand = CreateSameType(lastCommand);
      newCommand = lastCommand;

      if (newCommand is IHasScheme hasScheme)
      {
        var foundScheme = hasScheme.Scheme;
        if (scheme == null || scheme.CountPoints() == 0)
        {
          scheme = foundScheme;
        }
        else
        {
          if (CompareSchemes(scheme, foundScheme) == false)
          {
            scheme.GroupModels.AddRange(foundScheme.GroupModels);
          }
          else
          {
            model.Errors.Add(GeneralErrors.SchemeConflict(model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}"));
            scheme.GroupModels.Clear();
          }
        }
      }
      else
      {
        // у этой команды схемы нет — просто пропускаем/логируем
        LogInformation($"Команда {newCommand.GetType().Name} не содержит Scheme.");
      }
      return scheme;
    }

    public static bool CompareSchemes(SchemeModel modelScheme, SchemeModel addedScheme)
    {
      List<PointModel> allPoints = GetAllPoints(addedScheme);
      foreach (var point in allPoints)
      {
        foreach (var groupModel in modelScheme.GroupModels)
        {
          foreach (var chainModel in groupModel.ChainModels)
          {
            foreach (var pointModel in chainModel.PointModels)
            {
              if (pointModel.ToString() == point.ToString())
              {
                return true;
              }
            }
          }
        }
      }
      return false;
    }

    private static List<PointModel> GetAllPoints(SchemeModel addedScheme)
    {
      var allPoints = new List<PointModel>();
      foreach (var groupModel in addedScheme.GroupModels)
      {
        foreach (var chainModel in groupModel.ChainModels)
        {
          allPoints.AddRange(chainModel.PointModels);
        }
      }

      return allPoints;
    }

    /// <summary>
    /// Создаёт новый экземпляр команды того же типа, что и lastCommand.
    /// </summary>
    /// <typeparam name="T">Тип команды, наследник BaseCommandModel.</typeparam>
    /// <param name="lastCommand">Экземпляр команды, по которому определяется тип.</param>
    /// <returns>Новый экземпляр команды указанного типа.</returns>
    public static T CreateSameType<T>(T lastCommand) where T : BaseCommandModel
    {
      if (lastCommand == null)
        throw new ArgumentNullException(nameof(lastCommand));

      // Получаем реальный runtime-тип объекта
      Type commandType = lastCommand.GetType();

      // Создаём новый экземпляр того же типа
      var newCommand = Activator.CreateInstance(commandType);

      return (T)newCommand;
    }

    public static SchemeModel CheckKeyS(SchemeModel scheme)
    {
      return GetPointsFromPM(scheme);
    }

    public static SchemeModel CheckKeyP(BaseCommandModel model, SchemeModel scheme, BaseCommandModel modelSi = null)
    {
      var lastCommand = GetLastFromCheckCommands();
      if (lastCommand != null)
      {
        scheme = GetShemeFromLastCommand(model, scheme, lastCommand);

        if (modelSi != null)
        {
          if (modelSi.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
          {
            scheme = GetPointsFromPM(scheme);
          }
        }
        else
        {
          if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
          {
            scheme = GetPointsFromPM(scheme);
          }
        }
      }
      return scheme;
    }
  }
}

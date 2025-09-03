using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model.Chains;
using Utilities;
using Utilities.Models;

namespace ControlCommandAnalyser.Model
{
  public class CommandsModel
  {
    public static List<BaseCommandModel> CommandModels = new();
    public static List<string> CheckCommandMnemonic = new List<string> { "ПР", "ПИ", "СИ", "ПЭ", "ЭТ" };
    /// <summary>
    /// Получает последнюю команду из списка команд проверки.
    /// </summary>
    /// <returns>Найденную команду или null.</returns>
    public static BaseCommandModel GetLastFromCheckCommands()
    {
      return CommandModels.Where(comand => CheckCommandMnemonic.Contains(comand.Mnemonic)).ToList().Last();
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
    /// Статический метод для очистки списка моделей команд.
    /// </summary>
    public static void Clear()
    {
      CommandModels.Clear();
    }

    public static void GetPointsFromPM(SchemeModel model)
    {
      // добавляем просто точки из РМ, которые еще не были записаны
      BaseCommandModel lastCommand = GetByMnemonic("РМ").Last();
      if (lastCommand is RmCommandModel)
      {
        List<PointModel> allPoints = GetAllPoints(model);

        var rmCommand = lastCommand as RmCommandModel;

        foreach (var point in rmCommand.PointsMap)
        {
          //var point = rmCommand.PointsMap.ToArray()[i];
          var parsedPoint = PointModel.ParsePointString(point.Value);
          bool next = false;
          foreach (var pointModel in allPoints)
          {
            if (pointModel.ToString() == parsedPoint.ToString())
            {
              next = true;
              break;
            }
          }
          if (next == false)
          {
            var chainModel = new ChainModel(new List<PointModel> { parsedPoint });
            var groupModel = new GroupModel(new List<ChainModel> { chainModel });
            allPoints.Add(parsedPoint);
            model.GroupModels.Add(groupModel);
          }
        }
      }
      LoggerUtility.LogInformation(
        $"Схема распознана из РМ: цепей={model.GroupModels?.Count ?? 0}, частей={model.CountParts()}, точек={model.CountPoints()}");
    }

    public static void GetShemeFromLastCommand(BaseCommandModel model, SchemeModel scheme, BaseCommandModel lastCommand)
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
          }
        }
      }
      else
      {
        // у этой команды схемы нет — просто пропускаем/логируем
        LoggerUtility.LogInformation($"Команда {newCommand.GetType().Name} не содержит Scheme.");
      }
    }

    public static bool CompareSchemes(SchemeModel modelScheme, SchemeModel addedScheme)
    {
      List<PointModel> allPoints = GetAllPoints(addedScheme);
      foreach (var groupModel in modelScheme.GroupModels)
      {
        foreach (var chainModel in groupModel.ChainModels)
        {
          foreach (var pointModel in chainModel.PointModels)
          {
            if (allPoints.Contains(pointModel))
            {
              return true;
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

    public static void CheckKeyS(SchemeModel scheme)
    {
      GetPointsFromPM(scheme);
    }

    public static void CheckKeyP(BaseCommandModel model, SchemeModel scheme)
    {
      var lastCommand = GetLastFromCheckCommands();
      if (lastCommand != null)
      {
        GetShemeFromLastCommand(model, scheme, lastCommand);

        if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
        {
          GetPointsFromPM(scheme);
        }
      }
    }
  }
}

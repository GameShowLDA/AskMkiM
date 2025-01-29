using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.ProtocolConfig;
using static AppConfig.Config.LoopConfig;
using static AppConfig.Config.SystemStateManager;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
using static Utilities.Models.ShowMessageModel;
using static Utilities.LoggerUtility;
using static Utilities.DelegateManager;
using Utilities.Models;
using Core.ConfigCollector;
using static AppConfig.Enums.ValidationEnum;
using Mode.Models;


namespace Mode.Base.SearchDevices
{
  internal class InputValidator
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;

    /// <summary>
    /// Асинхронно проверяет корректность заданной точки измерения.
    /// </summary>
    /// <param name="messageDelegate">Делегат для отображения сообщений.</param>
    /// <param name="pointText">Текст точки измерения для проверки.</param>
    /// <param name="isFirstPoint">True, если это первая точка, иначе False.</param>
    /// <returns>True, если точка корректна, иначе False.</returns>
    internal async Task<(bool IsSuccess, PointModel PointModel, Core.ManagerShassy.Model ManagerShassy, Core.ModuleRelayControl.Model ModuleRelayControl)> ValidateMeasurementPointAsync(MessageDelegate messageDelegate, string pointText)
    {
      Core.ManagerShassy.Model managerModel;
      Core.ModuleRelayControl.Model moduleRelayModel;

      var validationResult = ValidateMeasurePointAsync(pointText, out moduleRelayModel, out managerModel);
      if (validationResult == ValidationDataResult.Success)
      {
        PointModel pointModel = ParsePoint(pointText);
        return (true, pointModel, managerModel, moduleRelayModel);
      }
      else
      {
        await GetErrorMessage(messageDelegate, validationResult, pointText);
        return (false, null, null, null);
      }
    }

    /// <summary>
    /// Асинхронно проверяет корректность электрического параметра.
    /// </summary>
    /// <param name="messageDelegate">Делегат для отображения сообщений.</param>
    /// <returns>True, если параметр корректен, иначе False.</returns>
    internal async Task<bool> ValidateElectricalParameterAsync(MessageDelegate messageDelegate, string parameter)
    {
      LogInformation("Проверка сопротивления.");
      try
      {
        LogInformation("Проверка электрического параметра.");
        if (!double.TryParse(parameter, out _))
        {
          LogError("Ошибка: Невозможно преобразовать данные измерения сопротивления.");
          return false;
        }
        return true;
      }
      catch (Exception ex)
      {
        await messageDelegate(new ShowMessageModel("Ошибка в сопротивлении", errorText.Item2, "Проверьте корректность заполнения сопротивления."));
        LogError(ex.Message);
        return false;
      }
    }

    /// <summary>
    /// Проверяет корректность заданной точки измерения.
    /// </summary>
    /// <param name="pointText">Текст точки измерения для проверки.</param>
    /// <returns>Результат валидации.</returns>
    private static ValidationDataResult ValidateMeasurePointAsync(string pointText, out Core.ModuleRelayControl.Model moduleRelay, out Core.ManagerShassy.Model managerShassy)
    {
      moduleRelay = null;
      managerShassy = null;

      try
      {
        LogInformation($"Проверка точки измерения: {pointText}");
        var point = ParsePoint(pointText);
        if (point == null)
        {
          return ValidationDataResult.InvalidPointData;
        }

        if (!ValidateManagerShassyNumber(point, out managerShassy))
        {
          return ValidationDataResult.ManagerShassyNumberMissing;
        }
        else
        {
          managerShassy = ConfigCollector.GetManagerShassy();
        }

        ValidationDataResult result;
        if ((result = ValidateModuleAndPoint(point, ref moduleRelay)) != ValidationDataResult.Success)
        {
          return result;
        }

        LogInformation($"Точка {pointText} корректна.");
        return ValidationDataResult.Success;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка : {ex}");
        return ValidationDataResult.UnknownError;
      }
    }

    /// <summary>
    /// Проверяет точки на уникальность.
    /// </summary>
    /// <returns>True, если сопротивление корректно, иначе False.</returns>
    internal bool ValidateUniqueMeasurementPointAsync(PointModel firstMeasurementPoint, PointModel secondMeasurementPoint)
    {
      try
      {
        LogInformation("Проверка уникальности точек.");
        if (firstMeasurementPoint != null && secondMeasurementPoint != null &&
            firstMeasurementPoint.Equals(secondMeasurementPoint))
        {
          LogError("Ошибка: Вторая точка совпадает с первой.");
          return false;
        }
        return true;
      }
      catch (Exception ex)
      {
        LogError(ex.Message);
        return false;
      }
    }

    /// <summary>
    /// Парсит строку точки измерения в объект PointModel.
    /// </summary>
    /// <param name="pointText">Текст точки измерения.</param>
    /// <returns>Объект PointModel или null, если парсинг не удался.</returns>
    private static PointModel ParsePoint(string pointText)
    {
      var point = PointModel.ParsePointString(pointText);
      if (point == null)
      {
        LogError($"Ошибка: некорректная точка измерения '{pointText}'.");
      }
      return point;
    }

    /// <summary>
    /// Проверяет, задан ли менеджер шасси для устройства.
    /// </summary>
    /// <param name="point">Объект PointModel для проверки.</param>
    /// <returns>True, если менеджер шасси задан, иначе False.</returns>
    private static bool ValidateManagerShassyNumber(PointModel point, out Core.ManagerShassy.Model managerShassy)
    {
      managerShassy = null;
      var managerShassyNumber = ConfigCollector.GetManagerShassyNumber();
      if (string.IsNullOrEmpty(managerShassyNumber) || point.DeviceNumber.ToString(CultureInfo.InvariantCulture) != managerShassyNumber.ToString(CultureInfo.InvariantCulture))
      {
        LogError($"Ошибка: менеджер шасси не задан для устройства с номером {point.DeviceNumber}.");
        return false;
      }
      else
      {
        managerShassy = new Core.ManagerShassy.Model(IPAddress.Parse($"192.168.{point.DeviceNumber}.0"));
      }
      return true;
    }

    /// <summary>
    /// Проверяет, существует ли модуль и точка в конфигурации.
    /// </summary>
    /// <param name="point">Объект PointModel для проверки.</param>
    /// <returns>True, если модуль и точка существуют, иначе False.</returns>
    private static ValidationDataResult ValidateModuleAndPoint(PointModel point, ref Core.ModuleRelayControl.Model moduleRelay)
    {
      List<Core.ModuleRelayControl.Model> mkrList = ConfigCollector.GetMkrModels();
      if (mkrList == null || mkrList.Count < point.ModuleNumber)
      {
        LogError($"Ошибка: модуль {point.ModuleNumber} не найден в конфигурации.");
        return ValidationDataResult.ModuleNotFound;
      }
      else
      {
        var mkrCountPoints = mkrList[point.ModuleNumber - 1].CountPoints;
        if (point.PointNumber < 0 || point.PointNumber > mkrCountPoints)
        {
          LogError($"Ошибка: точка {point.PointNumber} выходит за пределы модуля {point.ModuleNumber}.");
          return ValidationDataResult.PointOutOfRange;
        }

        moduleRelay = mkrList[point.ModuleNumber - 1];
      }
      return ValidationDataResult.Success;
    }

    /// <summary>
    /// Асинхронно получает сообщение об ошибке для заданного результата валидации.
    /// </summary>
    /// <param name="messageDelegate">Делегат для отображения сообщений.</param>
    /// <param name="validationResult">Результат валидации.</param>
    /// <param name="isFirstPoint">True, если это первая точка, иначе False.</param>
    private async Task GetErrorMessage(MessageDelegate messageDelegate, ValidationDataResult validationResult, string point)
    {
      string errorMessage;

      switch (validationResult)
      {
        case ValidationDataResult.InvalidPointData:
          errorMessage = $"Проверьте корректность точки. Точка должна быть формата \"x.x.x\"";
          break;
        case ValidationDataResult.ManagerShassyNumberMissing:
          errorMessage = $"Выбранный вами \"Менеджер шасси\" не задан в конфигурации. Проверьте конфигурацию и повторите попытку.";
          break;
        case ValidationDataResult.ModuleNotFound:
          errorMessage = $"Выбранный вами \"Модуль коммутации реле\" не задан в конфигурации. Максимальный номер модуля при вашей конфигурации равен {ConfigCollector.GetMkrModels()?.Count ?? 0}";
          break;
        case ValidationDataResult.PointOutOfRange:
          errorMessage = $"Точка выходит за пределы указанных точек в конфигурации.";
          break;
        case ValidationDataResult.UnknownError:
          errorMessage = $"Системная ошибка. Обратитесь за помощью к администратору.";
          break;
        default:
          errorMessage = "Неизвестная ошибка.";
          break;
      }

      await messageDelegate(new ShowMessageModel($"Ошибка {point} точки", errorText.Item2, errorMessage));
    }
  }
}

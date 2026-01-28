using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using Newtonsoft.Json.Linq;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  /// <summary>
  /// Класс <see cref="ConnectedPointChecker"/> предназначен для проверки электрической
  /// последовательности соединённых точек в схеме и выявления разрывов цепей.
  /// </summary>
  internal static class ConnectedPointChecker
  {
    /// <summary>
    /// Делегат, определяющий метод выполнения измерений.
    /// </summary>
    /// <param name="value">Заданное значение сопротивления для измерения.</param>
    /// <param name="userMessageService">Сервис для отображения сообщений пользователю.</param>
    /// <param name="cancellationToken">Токен отмены для управления асинхронной операцией.</param>
    /// <returns>
    /// Асинхронная операция, возвращающая <c>true</c>, если измерение прошло успешно,
    /// или <c>false</c>, если обнаружена ошибка.
    /// </returns>
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserInteractionService userMessageService, CancellationToken cancellationToken, double errorResistance);

    /// <summary>
    /// Асинхронно выполняет проверку соединённых точек в схеме.
    /// </summary>
    /// <param name="schemeModel">Модель схемы, содержащая список соединённых точек.</param>
    /// <param name="performMeasurementAsync">
    /// Делегат для выполнения измерения сопротивления между базовой и проверяемой точками.
    /// </param>
    /// <param name="manager">
    /// Экземпляр <see cref="CommandExecutionManager"/>, используемый для регистрации ошибок выполнения.
    /// </param>
    /// <param name="baseCommandModel">
    /// Модель базовой команды, содержащая данные для формирования ошибок.
    /// </param>
    /// <param name="messageService">Сервис для отображения сообщений пользователю.</param>
    /// <param name="resistance">Заданное значение сопротивления для проверки.</param>
    /// <returns>
    /// Асинхронная операция, возвращающая список сообщений об ошибках
    /// (<see cref="ShowMessageModel"/>), если были обнаружены разрывы цепей;
    /// в противном случае возвращается пустой список.
    /// </returns>
    static public async Task<List<ShowMessageModel>> CheckSequenceAsync(ConnectedPointContext context)
    {
      List<ShowMessageModel> errorsMessage = new List<ShowMessageModel>();
      Dictionary<List<PointModel>, string> errorChain = new();
      var pointsList = context.SchemeModel.GetPointsConnected();
      if (pointsList.Count == 0)
      {
        return errorsMessage;
      }

      await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.MessageRelativeToFirstPoint));

      for (int i = 0; i < pointsList.Count; i++)
      {
        var chains = pointsList[i];

        for (int j = 0; j < chains.ChainModels.Count; j++)
        {
          var points = chains.ChainModels[j];
          string chainsStr = "*";
          for (int z = 0; z < points.PointModels.Count; z++)
          {
            if ((z + 1) == points.PointModels.Count)
            {
              if (await DeviceDisplayConfig.GetMachineAddressVisibilityAsync())
              {
                chainsStr += $"{points.PointModels[z].Mnemonic}[{points.PointModels[z].ToString()}]*";
              }
              else
              {
                chainsStr += $"{points.PointModels[z].Mnemonic}*";
              }
            }
            else
            {
              if (await DeviceDisplayConfig.GetMachineAddressVisibilityAsync())
              {
                chainsStr += $"{points.PointModels[z].Mnemonic}[{points.PointModels[z].ToString()}],";
              }
              else
              {
                chainsStr += $"{points.PointModels[z].Mnemonic},";
              }
            }
          }

          await context.MessageService.AppendEmptyLineAsync();
          await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(chainsStr), IsBlockStart: true);
          var _basePoint = points.PointModels[0];
          points.PointModels.Remove(_basePoint);

          await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек") { IndentLevel = 1 }, IsBlockStart: true);
          await DeviceManager.ConnectPointToBusBAsync(_basePoint, context.MessageService, false);


          foreach (var point in points.PointModels)
          {
            context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();

            string _baseMachineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"({_basePoint.ToString()})" : string.Empty;
            string machineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"({point.ToString()})" : string.Empty;

            await context.MessageService.ShowMessageAsync(await ExecutorMessageBuilder.BuildPointsCheckHeaderAsync(_basePoint, point, CircuitFaultType.ShortCircuit), IsBlockStart: true);
            await DeviceManager.ConnectPointToBusAAsync(point, context.MessageService, false);

            var module = EquipmentService.GetModuleByPoint(point);
            var result = await context.PerformMeasurementAsync(context.Value, context.MessageService, context.MessageService.GetCancellationToken(), module.SwitchResistance);
            if (!result.Result)
            {
              var item = new List<PointModel>() { _basePoint, point };
              errorChain.Add(item, result.Value.ToString());

              var chain = new ChainModel(item);
              var chainStr = await context.CommandModel.BuildDislpayInfo.BuildErrorChainStringAsync(chain);

              context.CommandManager.AddErrorMethod(context.CommandModel.PointErrors.DisconnectChainError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", chainStr, $"{result.Value.ToString()} Ом", context.CommandModel.StartLineNumber, context.CommandModel.FormattedStartLineNumber));
            }

            await DeviceManager.DisconnectPointFromBusAAsync(point, context.MessageService, false);
          }
          await DeviceManager.DisconnectPointFromBusBAsync(_basePoint, context.MessageService, false);
        }
      }

      if (errorChain.Count > 0)
      {
        await context.MessageService.ShowMessageAsync(
          new ShowMessageModel($"Результаты проверки")
          { IndentLevel = 1 });


        foreach (var item in errorChain.Keys)
        {
          var chain = new ChainModel(item);
          var chainStr = await context.CommandModel.BuildDislpayInfo.BuildErrorChainStringAsync(chain);

          var overload = errorChain.GetValueOrDefault(item) != "9,9E+37" ? false : true;
          var error = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, Convert.ToDouble(errorChain.GetValueOrDefault(item)), chainStr, overload: overload);
          error.Status = ShowMessageModel.MessageType.Error;
          error.IndentLevel = 2;

          await context.MessageService.ShowMessageAsync(error);
          errorsMessage.Add(error);
          await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {error.ToString()}"));
        }
      }

      return errorsMessage; 

    }
  }
}

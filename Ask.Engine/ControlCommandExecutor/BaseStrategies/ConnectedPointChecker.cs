using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

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
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserInteractionService userMessageService, CancellationToken cancellationToken);

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

        for (int j = 0; j < chains.Count; j++)
        {
          var points = chains[j];
          string chainsStr = "*";
          for (int z = 0; z < points.Count; z++)
          {
            if ((z + 1) == points.Count)
            {
              if (await DeviceDisplayConfig.GetMachineAddressVisibilityAsync())
              {
                chainsStr += $"{points[z].Mnemonic}[{points[z].ToString()}]*";
              }
              else
              {
                chainsStr += $"{points[z].Mnemonic}*";
              }
            }
            else
            {
              if (await DeviceDisplayConfig.GetMachineAddressVisibilityAsync())
              {
                chainsStr += $"{points[z].Mnemonic}[{points[z].ToString()}],";
              }
              else
              {
                chainsStr += $"{points[z].Mnemonic},";
              }
            }
          }

          await context.MessageService.AppendEmptyLineAsync();
          await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(chainsStr), IsBlockStart: true);
          var _basePoint = points[0];
          points.Remove(_basePoint);

          await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек") { IndentLevel = 1 }, IsBlockStart: true);
          await DeviceManager.ConnectPointToBusBAsync(_basePoint, context.MessageService);


          foreach (var point in points)
          {
            context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();

            string _baseMachineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"({_basePoint.ToString()})" : string.Empty;
            string machineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"({point.ToString()})" : string.Empty;

            await context.MessageService.ShowMessageAsync(await ExecutorMessageBuilder.BuildPointsCheckHeaderAsync(_basePoint, point, CircuitFaultType.ShortCircuit), IsBlockStart: true);
            await DeviceManager.ConnectPointToBusAAsync(point, context.MessageService);

            var result = await context.PerformMeasurementAsync(context.Value, context.MessageService, context.MessageService.GetCancellationToken());
            if (!result.Result)
            {
              var item = new List<PointModel>() { _basePoint, point };
              errorChain.Add(item, result.Value.ToString());

              var chain = new ChainModel(item);
              var chainStr = await context.CommandModel.BuildDislpayInfo.BuildErrorChainStringAsync(chain);

              context.CommandManager.AddErrorMethod(context.CommandModel.PointErrors.DisconnectChainError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", chainStr, $"{result.Value.ToString()} Ом", context.CommandModel.StartLineNumber, context.CommandModel.FormattedStartLineNumber));
            }

            await DeviceManager.DisconnectPointFromBusAAsync(point, context.MessageService);
          }
          await DeviceManager.DisconnectPointFromBusBAsync(_basePoint, context.MessageService);
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

          ShowMessageModel error = null;

          if (context.HigherLimit > 0)
          {
            error = new ShowMessageModel($"{chainStr} ({context.LowerLimit} - {context.HigherLimit} {context.Unit})", message: $"{context.UnitMnemonic}изм = {errorChain.GetValueOrDefault(item)} {context.Unit}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 };
          }
          else
          {
            error = new ShowMessageModel($"{chainStr} ({context.LowerLimit} - ∞ {context.Unit})", message: $"{context.UnitMnemonic}изм = {errorChain.GetValueOrDefault(item)} {context.Unit}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 };
          }

          await context.MessageService.ShowMessageAsync(error);
          errorsMessage.Add(error);
          await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {error.ToString()}"));
        }
      }

      return errorsMessage;
    }
  }
}

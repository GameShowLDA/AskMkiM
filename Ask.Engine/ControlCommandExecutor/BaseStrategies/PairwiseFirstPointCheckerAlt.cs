using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  internal class PairwiseFirstPointCheckerAlt
  {
    /// <summary>
    /// Выполняет последовательную проверку точек относительно первой.
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task<List<ShowMessageModel>> CheckSequenceAsync(SchemeModel schemeModel, CommandExecutionManager manager, BaseCommandModel baseCommandModel, IUserInteractionService messageService, double resistance = 0, double cabelResistance = 0)
    {
      List<ShowMessageModel> errorsMessgae = new List<ShowMessageModel>();

      List<List<ChainModel>> errorChain = new();
      var pointsListSource = schemeModel.GetPointsConnected();
      if (pointsListSource.Count == 0)
      {
        return errorsMessgae;
      }

      await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.DisconnectionRelativeToFirstPoint));

      foreach (var groups in pointsListSource)
      {
        foreach (var chains in groups)
        {
          bool errorPoint = false;
          var str = string.Empty;

          foreach (var point in chains)
          {
            str += $"{EquipmentService.GetPointKey(point)},";
          }
          str = str.Remove(str.Length - 1);

          await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(str));

          var _basePoint = chains.First();
          await ConnectToBusAAndBAsync(messageService, _basePoint);

          var Rt1 = await GetResistanceAsync(messageService, resistance);
          if (Rt1 > 100)
          {
            var machineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"[{_basePoint.ToString()}]" : string.Empty;

            var errorMessageModels = new ShowMessageModel($"{_basePoint.Mnemonic}{machineAdress}", message: "Rизм = Нет подлючения точки", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 };
            errorPoint = true;

            await messageService.ShowMessageAsync(errorMessageModels);
            errorsMessgae.Add(errorMessageModels);
            await messageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
            manager.AddErrorMethod(EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", $"{_basePoint}{machineAdress}", messageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));
          }
          else
          {
            var machineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"[{_basePoint.ToString()}]" : string.Empty;
            if (await DeviceDisplayConfig.GetMeasurementResultsVisibilityAsync())
            {
              await messageService.ShowMessageAsync(new ShowMessageModel($"{_basePoint.Mnemonic}{machineAdress}", message: $"Rизм = {Rt1:F5} Ом", type: ShowMessageModel.MessageType.Success) { IndentLevel = 1 });
            }
          }

          await DeviceManager.DisconnectPointFromBusBAsync(_basePoint, messageService);

          for (int i = 1; i < chains.Count; i++)
          {
            var point = chains[i];
            await ConnectToBusAAndBAsync(messageService, point);

            var Rt2 = await GetResistanceAsync(messageService, resistance);
            if (Rt2 > 100)
            {
              var machineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"[{point.ToString()}]" : string.Empty;

              var errorMessageModels = new ShowMessageModel($"{point.Mnemonic}{machineAdress}", message: $"Нет подлючения точки", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 };
              errorPoint = true;

              await messageService.ShowMessageAsync(errorMessageModels);
              errorsMessgae.Add(errorMessageModels);
              manager.AddErrorMethod(EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", $"{point.Mnemonic}{machineAdress}", messageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));
              await messageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
            }
            else
            {
              var machineAdress = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"[{point.ToString()}]" : string.Empty;
              if (await DeviceDisplayConfig.GetMeasurementResultsVisibilityAsync())
              {
                await messageService.ShowMessageAsync(new ShowMessageModel($"{point.Mnemonic}{machineAdress}", message: $"Rизм = {Rt2:F5} Ом", type: ShowMessageModel.MessageType.Success) { IndentLevel = 1 });
              }
            }

            await DeviceManager.DisconnectPointFromBusAAsync(point, messageService);
            
            double Rt = -1;
            var LowerBound = (baseCommandModel as EhtCommandModel).LowerLimitResistance.Value;
            var UpperBound = (baseCommandModel as EhtCommandModel).HigherLimitResistance.Value;

            string machineAdressFirst = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"[{_basePoint.ToString()}]" : string.Empty;
            string machineAdressSecond = await DeviceDisplayConfig.GetMachineAddressVisibilityAsync() ? $"[{point.ToString()}]" : string.Empty;

            if (!errorPoint)
            {
              Rt = await GetResistanceAsync(messageService, resistance);

              if (Rt > 100)
              {
                var errorMessageModels = new ShowMessageModel($"{_basePoint.Mnemonic}{machineAdressFirst}, {point.Mnemonic}{machineAdressSecond}", message: $"Overload", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 };
                errorPoint = true;

                await messageService.ShowMessageAsync(errorMessageModels);
                manager.AddErrorMethod(EhtErrors.CircuitOverload($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", $"{_basePoint.Mnemonic}{machineAdressFirst}", $"{point.Mnemonic}{machineAdressSecond}", messageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));
                errorsMessgae.Add(errorMessageModels);
                await messageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
              }
              else
              {
                await messageService.ShowMessageAsync(new ShowMessageModel($"{_basePoint.Mnemonic}{machineAdressFirst}, {point.Mnemonic}{machineAdressSecond}", message: $"{Rt:F5} Ом") { IndentLevel = 1 });
              }
            }

            await DeviceManager.DisconnectPointFromBusBAsync(point, messageService);
            if (!errorPoint)
            {
              await messageService.ShowMessageAsync(new ShowMessageModel("Итог измерений"));

              double Rx = 0;
              if (!errorPoint)
              {
                Rx = Rt - ((Rt1 + Rt2) / 2);
              }
              else
              {
                Rx = Rt;
              }

              var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? Rx : !await ExecutionConfig.GetIsErrorSimulationEnabled() ? (LowerBound + UpperBound) / 2 : Rx;

              result -= cabelResistance;

              if (result < 0)
              {
                result = 0;
              }

              var succes = result >= LowerBound && result <= UpperBound;

              var error = new ShowMessageModel(
                $"{_basePoint.Mnemonic}{machineAdressFirst},{point.Mnemonic}{machineAdressSecond} ({LowerBound} - {UpperBound} Ом)",
                message: $"Rизм = {result:F5} Ом",
                type: succes ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)
              { IndentLevel = 3 };

              await messageService.ShowMessageAsync(error);

              if (!succes)
              {
                errorsMessgae.Add(error);
                manager.AddErrorMethod(EhtErrors.ResistanceOutOfRange($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", result, _basePoint.ToString(), point.ToString(), LowerBound, UpperBound, messageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));

                await messageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {error.ToString()}"));
              }
            }
          }

          await DisconnectAllPoints(messageService, chains);
        }
      }

      return errorsMessgae;
    }

    static private async Task ConnectToBusAAndBAsync(IUserInteractionService userMessageService, PointModel pointModel)
    {
      await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Подключение точки {pointModel.ToString()} к шинам А и В"), IsBlockStart: true);
      var relayModule = EquipmentService.GetModuleByPoint(pointModel);

      await relayModule.PointManager.ConnectRelayAsync(BusPoint.A, pointModel.PointNumber, userMessageService);
      await relayModule.PointManager.ConnectRelayAsync(BusPoint.B, pointModel.PointNumber, userMessageService);
    }

    static private async Task<double> GetResistanceAsync(IUserInteractionService userMessageService, double param)
    {
      var fastMeter = EquipmentService.GetFastMeterOrThrow(userMessageService);
      await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Измерение сопротивления"), IsBlockStart: true);
      var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? await fastMeter.ResistanceManager.MeasureResistanceAsync() : !await ExecutionConfig.GetIsErrorSimulationEnabled() ? param / 2 : new Random().Next((int)param - 100, (int)param + 100);
      return result;
    }

    static private async Task DisconnectAllPoints(IUserInteractionService userMessageService, List<PointModel> points)
    {
      var modules = EquipmentService.GetUniqueModulesByPoints(points);
      foreach (var module in modules)
      {
        await module.PointManager.DisconnectingAllPoint(userMessageService);
      }
    }
  }
}

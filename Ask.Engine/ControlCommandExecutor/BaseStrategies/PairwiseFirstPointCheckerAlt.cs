using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

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
    static public async Task<(List<ShowMessageModel> errorMessage, List<ShowMessageModel> infoMessage)> CheckSequenceAsync(PairwiseFirstPointAltContext context)
    {
      List<ShowMessageModel> errorsMessgae = new List<ShowMessageModel>();
      List<ShowMessageModel> infoMessage = new List<ShowMessageModel>();
      var baseCommandModel = context.CommandModel;

      List <List<ChainModel>> errorChain = new();
      var pointsListSource = context.SchemeModel.GetPointsConnected();
      if (pointsListSource.Count == 0)
      {
        return (errorsMessgae, infoMessage);
      }

      await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.DisconnectionRelativeToFirstPoint));

      foreach (var groups in pointsListSource)
      {
        context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
        foreach (var chains in groups.ChainModels)
        {
          bool errorPoint = false;
          var str = string.Empty;

          foreach (var points in chains.PointModels)
          {
            str += $"{EquipmentService.GetPointKey(points)},";
          }
          str = str.Remove(str.Length - 1);

          await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(str));

          var _basePoint = chains.PointModels.First();
          await ConnectToBusAAndBAsync(context.MessageService, _basePoint);

          var Rt1 = await GetResistanceAsync(context.MessageService, context.Value);
          if (Rt1 > 100)
          {
            var machineAdress = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{_basePoint.ToString()}]" : string.Empty;

            var errorMessageModels = new ShowMessageModel($"{_basePoint.Mnemonic}{machineAdress}", message: "Rизм = Нет подлючения точки", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 };
            errorPoint = true;

            await context.MessageService.ShowMessageAsync(errorMessageModels);
            errorsMessgae.Add(errorMessageModels);
            await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
            context.CommandManager.AddErrorMethod(EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", $"{_basePoint}{machineAdress}", context.MessageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));
          }
          else
          {
            var machineAdress = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{_basePoint.ToString()}]" : string.Empty;
            if (DeviceDisplayConfig.GetMeasurementResultsVisibility())
            {
              await context.MessageService.ShowMessageAsync(new ShowMessageModel($"{_basePoint.Mnemonic}{machineAdress}", message: $"Rизм = {Rt1:F5} Ом", type: ShowMessageModel.MessageType.Success) { IndentLevel = 1 });
            }
          }

          await DeviceManager.DisconnectPointFromBusBAsync(_basePoint, context.MessageService, context.IsPolarityReversed);

          for (int i = 1; i < chains.PointModels.Count; i++)
          {
            context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
            var point = chains.PointModels[i];
            await ConnectToBusAAndBAsync(context.MessageService, point);

            var Rt2 = await GetResistanceAsync(context.MessageService, context.Value);
            if (Rt2 > 100)
            {
              var machineAdress = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{point.ToString()}]" : string.Empty;

              var errorMessageModels = new ShowMessageModel($"{point.Mnemonic}{machineAdress}", message: $"Нет подлючения точки", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 };
              errorPoint = true;

              await context.MessageService.ShowMessageAsync(errorMessageModels);
              errorsMessgae.Add(errorMessageModels);
              context.CommandManager.AddErrorMethod(EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", $"{point.Mnemonic}{machineAdress}", context.MessageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));
              await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
            }
            else
            {
              var machineAdress = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{point.ToString()}]" : string.Empty;
              if (DeviceDisplayConfig.GetMeasurementResultsVisibility())
              {
                await context.MessageService.ShowMessageAsync(new ShowMessageModel($"{point.Mnemonic}{machineAdress}", message: $"Rизм = {Rt2:F5} Ом", type: ShowMessageModel.MessageType.Success) { IndentLevel = 1 });
              }
            }

            await DeviceManager.DisconnectPointFromBusAAsync(point, context.MessageService, context.IsPolarityReversed);

            double Rt = -1;
            var LowerBound = (baseCommandModel as EhtCommandModel).LowerLimitResistance.Value;
            var UpperBound = (baseCommandModel as EhtCommandModel).HigherLimitResistance.Value;

            string machineAdressFirst = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{_basePoint.ToString()}]" : string.Empty;
            string machineAdressSecond = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{point.ToString()}]" : string.Empty;

            if (!errorPoint)
            {
              Rt = await GetResistanceAsync(context.MessageService, context.Value);

              if (Rt > 100)
              {
                var errorMessageModels = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, Rt, chains: $"{_basePoint.Mnemonic}{machineAdressFirst}, {point.Mnemonic}{machineAdressSecond}");
                errorMessageModels.Status = ShowMessageModel.MessageType.Error;
                errorMessageModels.IndentLevel = 1;
                errorPoint = true;

                await context.MessageService.ShowMessageAsync(errorMessageModels);
                context.CommandManager.AddErrorMethod(EhtErrors.CircuitOverload($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", $"{_basePoint.Mnemonic}{machineAdressFirst}", $"{point.Mnemonic}{machineAdressSecond}", context.MessageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));
                errorsMessgae.Add(errorMessageModels);
                await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
              }
              else
              {
                await context.MessageService.ShowMessageAsync(new ShowMessageModel($"{_basePoint.Mnemonic}{machineAdressFirst}, {point.Mnemonic}{machineAdressSecond}", message: $"{Rt:F5} Ом") { IndentLevel = 1 });
              }
            }

            await DeviceManager.DisconnectPointFromBusBAsync(point, context.MessageService, context.IsPolarityReversed);
            if (!errorPoint)
            {
              await context.MessageService.ShowMessageAsync(new ShowMessageModel("Итог измерений"));

              double Rx = 0;
              if (!errorPoint)
              {
                Rx = Rt - ((Rt1 + Rt2) / 2);
              }
              else
              {
                Rx = Rt;
              }

              var result = !ExecutionConfig.GetIsIdleModeEnabled() ? Rx : !await ExecutionConfig.GetIsErrorSimulationEnabled() ? (LowerBound + UpperBound) / 2 : Rx;

              result -= context.CabelResistance;

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

              await context.MessageService.ShowMessageAsync(error);

              if (!succes)
              {
                errorsMessgae.Add(error);
                context.CommandManager.AddErrorMethod(EhtErrors.ResistanceOutOfRange($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", result, _basePoint.ToString(), point.ToString(), LowerBound, UpperBound, context.MessageService.GetLastLineNumber(), baseCommandModel.FormattedStartLineNumber));

                await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {error.ToString()}"));
              }

              if (context.IsProtocolAttribute)
              {
                infoMessage.Add(ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, result, $"{_basePoint.Mnemonic}{machineAdressFirst},{point.Mnemonic}{machineAdressSecond}"));
              }
            }
          }

          await DisconnectAllPoints(context.MessageService, chains);
        }
      }

      return (errorsMessgae, infoMessage);
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
      var result = !ExecutionConfig.GetIsIdleModeEnabled() ? await fastMeter.ContinuityManager.CheckContinuityAsync(param) : !await ExecutionConfig.GetIsErrorSimulationEnabled() ? param / 2 : new Random().Next((int)param - 100, (int)param + 100);
      return result;
    }

    static private async Task DisconnectAllPoints(IUserInteractionService userMessageService, ChainModel chain)
    {
      var modules = EquipmentService.GetUniqueModulesByPoints(chain.PointModels);
      foreach (var module in modules)
      {
        await module.PointManager.DisconnectingAllPoint(userMessageService);
      }
    }
  }
}

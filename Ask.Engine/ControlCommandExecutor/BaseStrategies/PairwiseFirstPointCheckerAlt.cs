using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
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

      List<List<ChainModel>> errorChain = new();
      var pointsListSource = context.SchemeModel.GetPointsConnected();
      if (pointsListSource.Count == 0)
      {
        return (errorsMessgae, infoMessage);
      }

      if (ProtocolConfig.GetTestStepMessagesInProtocol())
      {
        await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.DisconnectionRelativeToFirstPoint, context.IsPolarityReversed));
      }

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

          if (ProtocolConfig.GetTestStepMessagesInProtocol())
          {
            await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(str));
          }

          var _basePoint = chains.PointModels.First();
          await ConnectToBusAAndBAsync(context.MessageService, _basePoint);

          var Rt1 = await GetResistanceAsync(context.MessageService, context.Value, context.LowerLimit, context.HigherLimit);
          if (Rt1 > 100)
          {
            string machineAddress = string.Empty;

            if (DeviceDisplayConfig.GetMachineAddressVisibility())
            {
              if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
              {
                machineAddress = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(_basePoint.ToString())}]";
              }
              else
              {
                machineAddress = $"[{_basePoint.ToString()}]";
              }
            }

            var errorMessageModels = new ShowMessageModel($"{_basePoint.Mnemonic}{machineAddress}", message: "Rизм = Нет подлючения точки", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 };
            errorPoint = true;

            await context.MessageService.ShowMessageAsync(new ShowMessageModel(header: $"Результат измерений"));
            await context.MessageService.ShowMessageAsync(errorMessageModels);

            errorsMessgae.Add(errorMessageModels);
            await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
            context.CommandManager.AddErrorMethod(
              EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
              $"{_basePoint}{machineAddress}",
              context.MessageService.GetLastLineNumber(),
              baseCommandModel.FormattedStartLineNumber));
          }
          else
          {
            string machineAddress = string.Empty;

            if (DeviceDisplayConfig.GetMachineAddressVisibility())
            {
              if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
              {
                machineAddress = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(_basePoint.ToString())}]";
              }
              else
              {
                machineAddress = $"[{_basePoint.ToString()}]";
              }
            }

            if (DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility())
            {
              await context.MessageService.ShowMessageAsync(
                new ShowMessageModel(
                  $"Результат измерений ({_basePoint.Mnemonic}{machineAddress})",
                  message: MeasurementValueFormatter.FormatWithUnit(Rt1, "Ом"),
                  type: ShowMessageModel.MessageType.Info)
                { IndentLevel = 1 });
            }
          }

          await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(_basePoint, context.MessageService, context.IsPolarityReversed);

          for (int i = 1; i < chains.PointModels.Count; i++)
          {
            context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
            var point = chains.PointModels[i];
            await ConnectToBusAAndBAsync(context.MessageService, point);

            var Rt2 = await GetResistanceAsync(context.MessageService, context.Value, context.LowerLimit, context.HigherLimit);
            if (Rt2 > 100)
            {
              string machineAdress = string.Empty;
              if (DeviceDisplayConfig.GetMachineAddressVisibility())
              {
                if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
                {
                  machineAdress = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(point.ToString())}]";
                }
                else
                {
                  machineAdress = $"[{point.ToString()}]";
                }
              }

              var errorMessageModels = new ShowMessageModel($"{point.Mnemonic}{machineAdress}", message: $"Нет подлючения точки", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 };
              errorPoint = true;

              await context.MessageService.ShowMessageAsync(new ShowMessageModel(header: $"Измерение сопротивления"));
              await context.MessageService.ShowMessageAsync(errorMessageModels);
              errorsMessgae.Add(errorMessageModels);
              context.CommandManager.AddErrorMethod(
                EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
                $"{point.Mnemonic}{machineAdress}",
                context.MessageService.GetLastLineNumber(),
                baseCommandModel.FormattedStartLineNumber));

              await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
            }
            else
            {
              string machineAdress = string.Empty;
              if (DeviceDisplayConfig.GetMachineAddressVisibility())
              {
                if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
                {
                  machineAdress = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(point.ToString())}]";
                }
                else
                {
                  machineAdress = $"[{point.ToString()}]";
                }
              }

              if (DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility())
              {
                await context.MessageService.ShowMessageAsync(
                  new ShowMessageModel(
                    $"Результат измерений ({point.Mnemonic}{machineAdress})",
                    message: MeasurementValueFormatter.FormatWithUnit(Rt2, "Ом"),
                    type: ShowMessageModel.MessageType.Info)
                  { IndentLevel = 1 });
              }
            }

            await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(point, context.MessageService, context.IsPolarityReversed);

            double Rt = -1;
            var LowerBound = (baseCommandModel as EhtCommandModel).LowerLimitResistance.Value;
            var UpperBound = (baseCommandModel as EhtCommandModel).HigherLimitResistance.Value;

            string machineAdressFirst = string.Empty;
            if (DeviceDisplayConfig.GetMachineAddressVisibility())
            {
              if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
              {
                machineAdressFirst = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(_basePoint.ToString())}]";
              }
              else
              {
                machineAdressFirst = $"[{_basePoint.ToString()}]";
              }
            }

            string machineAdressSecond = string.Empty;
            if (DeviceDisplayConfig.GetMachineAddressVisibility())
            {
              if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
              {
                machineAdressSecond = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(point.ToString())}]";
              }
              else
              {
                machineAdressSecond = $"[{point.ToString()}]";
              }
            }

            if (!errorPoint)
            {
              Rt = await GetResistanceAsync(context.MessageService, context.Value, context.LowerLimit, context.HigherLimit);

              if (Rt > 100)
              {
                var errorMessageModels = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, Rt, chains: $"{_basePoint.Mnemonic}{machineAdressFirst}, {point.Mnemonic}{machineAdressSecond}");
                errorMessageModels.Status = ShowMessageModel.MessageType.Error;
                errorMessageModels.IndentLevel = 1;
                errorPoint = true;

                await context.MessageService.ShowMessageAsync(new ShowMessageModel(header: $"Измерение сопротивления"));
                await context.MessageService.ShowMessageAsync(errorMessageModels);
                context.CommandManager.AddErrorMethod(
                  EhtErrors.CircuitOverload($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
                  $"{_basePoint.Mnemonic}{machineAdressFirst}",
                  $"{point.Mnemonic}{machineAdressSecond}",
                  context.MessageService.GetLastLineNumber(),
                  baseCommandModel.FormattedStartLineNumber));

                errorsMessgae.Add(errorMessageModels);
                await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {errorMessageModels.ToString()}"));
              }
              else
              {
                if (DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility())
                {
                  await context.MessageService.ShowMessageAsync(
                    new ShowMessageModel(
                      $"Результат измерений ({_basePoint.Mnemonic}{machineAdressFirst},{point.Mnemonic}{machineAdressSecond})",
                      message: MeasurementValueFormatter.FormatWithUnit(Rt, "Ом"),
                      type: ShowMessageModel.MessageType.Info)
                    { IndentLevel = 1 });
                }
              }
            }

            await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(point, context.MessageService, context.IsPolarityReversed);
            if (!errorPoint)
            {
              double Rx = 0;
              if (!errorPoint)
              {
                Rx = Rt - ((Rt1 + Rt2) / 2);
              }
              else
              {
                Rx = Rt;
              }

              double result = 0;

              if (ExecutionConfig.GetIsIdleModeEnabled())
              {
                if (!await ExecutionConfig.GetIsErrorSimulationEnabled())
                {
                  result = (LowerBound + UpperBound) / 2;
                }
                else
                {
                  result = Rx;
                }
              }
              else
              {
                result = Rx;
              }

              if (!ExecutionConfig.GetIsIdleModeEnabled())
              {
                result -= context.CabelResistance;
              }

              if (result < 0)
              {
                result = 0;
              }

              var succes = result >= LowerBound && result <= UpperBound;

              var message = new ShowMessageModel(
                $"{_basePoint.Mnemonic}{machineAdressFirst},{point.Mnemonic}{machineAdressSecond} ({LowerBound} - {UpperBound} Ом)",
                message: $"Rизм = {MeasurementValueFormatter.Format(result)} Ом",
                type: succes ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error);

              if (DeviceDisplayConfig.GetMeasurementResultsVisibility() || !succes)
              {
                await context.MessageService.ShowMessageAsync(message);
              }

              if (!succes)
              {
                errorsMessgae.Add(message);
                context.CommandManager.AddErrorMethod(
                  EhtErrors.ResistanceOutOfRange($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
                  result,
                  _basePoint.ToString(),
                  point.ToString(),
                  LowerBound,
                  UpperBound,
                  context.MessageService.GetLastLineNumber(),
                  baseCommandModel.FormattedStartLineNumber));

                await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {message.ToString()}"));
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
      var relayModule = EquipmentService.GetModuleByPoint(pointModel);
      await relayModule.PointManager.ConnectRelayAsync(BusPoint.AB, pointModel.PointNumber, userMessageService);
    }

    static private async Task<double> GetResistanceAsync(IUserInteractionService userMessageService, double param, double rangeFrom, double rangeTo)
    {
      var fastMeter = EquipmentService.GetFastMeterOrThrow(userMessageService);
      var result = await fastMeter.ContinuityManager.CheckContinuityAsync(param, rangeFrom, rangeTo);
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

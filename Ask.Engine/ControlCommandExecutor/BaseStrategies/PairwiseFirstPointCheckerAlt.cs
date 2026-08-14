using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
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
    static public async Task<AlgorithmExecutionResult> CheckSequenceAsync(PairwiseFirstPointAltContext context)
    {
      var messages = new AlgorithmExecutionResult(new(), new());
      var baseCommandModel = context.CommandModel;

      List<List<ChainModel>> errorChain = new();
      var pointsListSource = context.SchemeModel.GetPointsConnected();
      if (pointsListSource.Count == 0)
      {
        return messages;
      }

      await CommandMessages.PublishCheckBlockHeaderAsync(
        context.MessageService,
        ControlCheckAlgorithm.DisconnectionRelativeToFirstPoint,
        context.IsPolarityReversed);

      foreach (var groups in pointsListSource)
      {
        context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
        foreach (var chains in groups.ChainModels)
        {
          bool basePointConnectionError = false;
          var str = string.Empty;

          foreach (var points in chains.PointModels)
          {
            str += $"{EquipmentService.GetPointKey(points)},";
          }
          str = str.Remove(str.Length - 1);

          await CommandMessages.PublishChainCheckBlockAsync(context.MessageService, str, isBlockStart: false);

          var _basePoint = chains.PointModels.First();
          await ConnectToBusAAndBAsync(context.MessageService, _basePoint);

          var Rt1 = context.ValidatePointConnections
            ? await GetResistanceAsync(context.MessageService, context.Value, context.LowerLimit, context.HigherLimit)
            : 0;
          if (context.ValidatePointConnections && Rt1 > 100)
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

            string measurementTarget = $"{_basePoint.Mnemonic}{machineAddress}";
            var errorMessageModels = MeasurementMessages.BuildPointConnectionError(measurementTarget);
            basePointConnectionError = true;

            await MeasurementMessages.PublishPointConnectionErrorAsync(CheckType.ControlProgram,
              measurementTarget,
              context.MessageService);

            messages.Errors.Add(errorMessageModels);
            await ExecutionMessages.PublishDebugAsync(
              $"Добавлена ошибка: {errorMessageModels}",
              context.MessageService);
            context.CommandManager.AddErrorMethod(
              EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
              $"{_basePoint}{machineAddress}",
              context.MessageService.GetLastLineNumber(),
              baseCommandModel.FormattedStartLineNumber));
          }
          else if (context.ValidatePointConnections)
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

            await MeasurementMessages.PublishIntermediateResultAsync(CheckType.ControlProgram,
              context.TypeCommand,
              new MeasurementRange(Rt1, context.LowerLimit, context.HigherLimit),
              true,
              $"{_basePoint.Mnemonic}{machineAddress}",
              outputService: context.MessageService);
          }

          await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(_basePoint, context.MessageService, context.IsPolarityReversed);

          for (int i = 1; i < chains.PointModels.Count; i++)
          {
            context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
            var point = chains.PointModels[i];
            bool currentPointError = false;
            await ConnectToBusAAndBAsync(context.MessageService, point);

            var Rt2 = context.ValidatePointConnections
              ? await GetResistanceAsync(context.MessageService, context.Value, context.LowerLimit, context.HigherLimit)
              : 0;
            if (context.ValidatePointConnections && Rt2 > 100)
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

              string measurementTarget = $"{point.Mnemonic}{machineAdress}";
              const string connectionError = "Нет подлючения точки";
              var errorMessageModels = MeasurementMessages.BuildPointConnectionError(
                measurementTarget,
                connectionError);
              currentPointError = true;

              await MeasurementMessages.PublishStartAsync(CheckType.ControlProgram,
                MeasurementTypeCommand.KC,
                context.MessageService);
              await MeasurementMessages.PublishPointConnectionErrorAsync(CheckType.ControlProgram,
                measurementTarget,
                context.MessageService,
                connectionError);
              messages.Errors.Add(errorMessageModels);
              context.CommandManager.AddErrorMethod(
                EhtErrors.PointNotConnected($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
                $"{point.Mnemonic}{machineAdress}",
                context.MessageService.GetLastLineNumber(),
                baseCommandModel.FormattedStartLineNumber));

              await ExecutionMessages.PublishDebugAsync(
                $"Добавлена ошибка: {errorMessageModels}",
                context.MessageService);
            }
            else if (context.ValidatePointConnections)
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

              await MeasurementMessages.PublishIntermediateResultAsync(CheckType.ControlProgram,
                context.TypeCommand,
                new MeasurementRange(Rt2, context.LowerLimit, context.HigherLimit),
                true,
                $"{point.Mnemonic}{machineAdress}",
                outputService: context.MessageService);
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

            if (CanMeasurePair(basePointConnectionError, currentPointError))
            {
              Rt = await GetResistanceAsync(context.MessageService, context.Value, context.LowerLimit, context.HigherLimit);

              if (context.ValidatePointConnections && Rt > 100)
              {
                var errorMessageModels = MeasurementMessages.BuildMeasurementResultMessage(
                  context.TypeCommand,
                  new MeasurementRange(Rt, context.LowerLimit, context.HigherLimit),
                  false,
                  $"{_basePoint.Mnemonic}{machineAdressFirst}, {point.Mnemonic}{machineAdressSecond}",
                  indentLevel: 1);
                currentPointError = true;

                await MeasurementMessages.PublishStartAsync(CheckType.ControlProgram,
                  MeasurementTypeCommand.KC,
                  context.MessageService);
                await MeasurementMessages.PublishResultAsync(CheckType.ControlProgram,
                  context.TypeCommand,
                  new MeasurementRange(Rt, context.LowerLimit, context.HigherLimit),
                  false,
                  $"{_basePoint.Mnemonic}{machineAdressFirst}, {point.Mnemonic}{machineAdressSecond}",
                  outputService: context.MessageService);
                context.CommandManager.AddErrorMethod(
                  EhtErrors.CircuitOverload($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
                  $"{_basePoint.Mnemonic}{machineAdressFirst}",
                  $"{point.Mnemonic}{machineAdressSecond}",
                  context.MessageService.GetLastLineNumber(),
                  baseCommandModel.FormattedStartLineNumber));

                messages.Errors.Add(errorMessageModels);
                await ExecutionMessages.PublishDebugAsync(
                  $"Добавлена ошибка: {errorMessageModels}",
                  context.MessageService);
              }
              else
              {
                await MeasurementMessages.PublishIntermediateResultAsync(CheckType.ControlProgram,
                  context.TypeCommand,
                  new MeasurementRange(Rt, context.LowerLimit, context.HigherLimit),
                  true,
                  $"{_basePoint.Mnemonic}{machineAdressFirst},{point.Mnemonic}{machineAdressSecond}",
                  outputService: context.MessageService);
              }
            }

            await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(point, context.MessageService, context.IsPolarityReversed);
            if (CanMeasurePair(basePointConnectionError, currentPointError))
            {
              double Rx = Rt - ((Rt1 + Rt2) / 2);

              double result = 0;

              if (ExecutionConfig.GetIsIdleModeEnabled())
              {
                if (IdleMeasurementErrorSimulator.TryGetValue(
                      LowerBound,
                      UpperBound,
                      out double erroneousValue))
                {
                  result = erroneousValue;
                }
                else
                {
                  result = (LowerBound / 2) + (UpperBound / 2);
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

              string measurementTarget = $"{_basePoint.Mnemonic}{machineAdressFirst},{point.Mnemonic}{machineAdressSecond}";
              var measurementRange = new MeasurementRange(result, LowerBound, UpperBound);
              var message = MeasurementMessages.BuildMeasurementResultMessage(
                ResistanceUnit.Ohm,
                measurementRange,
                succes,
                measurementTarget);
              await MeasurementMessages.PublishResultAsync(CheckType.ControlProgram,
                ResistanceUnit.Ohm,
                measurementRange,
                succes,
                measurementTarget,
                outputService: context.MessageService);

              if (!succes)
              {
                messages.Errors.Add(message);
                context.CommandManager.AddErrorMethod(
                  EhtErrors.ResistanceOutOfRange($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}",
                  result,
                  _basePoint.ToString(),
                  point.ToString(),
                  LowerBound,
                  UpperBound,
                  context.MessageService.GetLastLineNumber(),
                  baseCommandModel.FormattedStartLineNumber));

                await ExecutionMessages.PublishDebugAsync(
                  $"Добавлена ошибка: {message}",
                  context.MessageService);
              }

              if (context.IsProtocolAttribute)
              {
                messages.Info.Add(MeasurementMessages.BuildMeasurementResultMessage(
                  context.TypeCommand,
                  new MeasurementRange(result, context.LowerLimit, context.HigherLimit),
                  $"{_basePoint.Mnemonic}{machineAdressFirst},{point.Mnemonic}{machineAdressSecond}"));
              }
            }
          }

          await DisconnectAllPoints(context.MessageService, chains);
        }
      }

      return messages;
    }

    internal static bool CanMeasurePair(bool basePointConnectionError, bool currentPointError)
      => !basePointConnectionError && !currentPointError;

    static private async Task ConnectToBusAAndBAsync(IUserInteractionService userMessageService, PointModel pointModel)
    {
      var relayModule = EquipmentService.GetModuleByPoint(pointModel);
      await relayModule.PointManager.ConnectRelayAsync(BusPoint.AB, pointModel.PointNumber, userMessageService);
    }

    static private async Task<double> GetResistanceAsync(IUserInteractionService userMessageService, double param, double rangeFrom, double rangeTo)
    {
      var fastMeter = await EquipmentService.GetFastMeterOrThrow(userMessageService);

      MeasurementRange measurementRange = new MeasurementRange(param, rangeFrom, rangeTo);
      var result = await fastMeter.ContinuityManager.CheckContinuityAsync(measurementRange, userMessageService);

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

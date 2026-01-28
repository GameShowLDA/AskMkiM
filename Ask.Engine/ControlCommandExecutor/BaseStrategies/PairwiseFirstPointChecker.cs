using Ask.Core.Services.EventCore.Events;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Security.Claims;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  internal static class PairwiseFirstPointChecker
  {
    static private ChainModel _basePoint;

    /// <summary>
    /// Выполняет последовательную проверку точек относительно первой.
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task<List<ShowMessageModel>> CheckSequenceAsync(PairwiseFirstPointContext context)
    {
      List<ShowMessageModel> errorsMessage = new List<ShowMessageModel>();
      List<(ChainModel, ChainModel)> errorChains = new List<(ChainModel, ChainModel)>();

      var groupChains = context.SchemeModel.GetPointsDisconnected();
      if (groupChains.ChainModels.Count <= 1)
      {
        return errorsMessage;
      }

      _basePoint = groupChains.ChainModels.FirstOrDefault();
      var messageService = context.MessageService;

      await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.DisconnectionRelativeToFirstPoint));

      await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек"), IsBlockStart: true);

      await DeviceManager.ConnectChainToBusBAsync(_basePoint, messageService, context.IsPolarityReversed);
      groupChains.ChainModels.Remove(_basePoint);
      await messageService.ShowMessageAsync(new ShowMessageModel($"Выполнение измерений"), IsBlockStart: true);

      foreach (var chain in groupChains.ChainModels)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        string pointStr = string.Empty;
        var str = _basePoint.ToString();
        foreach (var point in chain.PointModels)
        {
          str += $"{EquipmentService.GetPointKey(point)},";
        }
        str = str.Remove(str.Length - 1);
        str += "*";

        await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(str), IsBlockStart: true);
        await DeviceManager.ConnectChainToBusAAsync(chain, messageService, context.IsPolarityReversed);

        var module = EquipmentService.GetModuleByPoint(chain.PointModels.FirstOrDefault());
        var measured = await context.PerformMeasurementAsync(context.Value, messageService, messageService.GetCancellationToken(), module.SwitchResistance, type: context.VoltageType);
        if (!measured.Result)
        {
          errorChains.Add((_basePoint, chain));
          var chainStr = await PointFormater.GetFormatDisconnectPoint(new List<ChainModel>() { _basePoint, chain });
          var err = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, measured.Value, chainStr);
          err.Status = ShowMessageModel.MessageType.Error;
          err.IndentLevel = 2;
          await messageService.ShowMessageAsync(err);

          errorsMessage.Add(err);
          context.CommandManager.AddErrorMethod(context.CommandModel.PointErrors.ChainError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", chainStr, context.CommandModel.StartLineNumber, context.CommandModel.FormattedStartLineNumber));
        }

        await DeviceManager.DisconnectChainFromBusAAsync(chain, messageService, context.IsPolarityReversed);
      }

      foreach (var item in errorChains)
      {
        var chainStr = await PointFormater.GetFormatDisconnectPoint(new List<ChainModel>() { item.Item1, item.Item2 });
        LogLib.LoggerUtility.LogDebug($"Замкнутая пара: {chainStr}");
      }

      if (context.IsInvokedByAnotherCommand)
      {
        context.SchemeModel.SetErrorChainDisconnectedPoints(errorChains);
      }
      return errorsMessage;
    }
  }
}


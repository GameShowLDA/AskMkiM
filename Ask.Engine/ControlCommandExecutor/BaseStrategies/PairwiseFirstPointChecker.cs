using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.Execution;

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
    static public async Task<List<ShowMessageModel>> CheckSequenceAsync(SchemeModel schemeModel, NodeAccumulationChecker.PerformMeasurementAsync performMeasurementAsync, CommandExecutionManager manager, BaseCommandModel baseCommandModel, IUserInteractionService messageService, double resistance = 0, VoltageEnum.Type type = VoltageEnum.Type.DCW)
    {
      List<ShowMessageModel> errorsMessgae = new List<ShowMessageModel>();

      List<List<ChainModel>> errorChain = new();
      var pointsList = schemeModel.GetPointsDisconnected();
      if (pointsList.Count == 0)
      {
        return errorsMessgae;
      }

      _basePoint = new ChainModel(pointsList.FirstOrDefault());

      await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.DisconnectionRelativeToFirstPoint));

      await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек"), IsBlockStart: true);

      await DeviceManager.ConnectChainToBusBAsync(_basePoint, messageService);
      pointsList.Remove(_basePoint.PointModels);
      await messageService.ShowMessageAsync(new ShowMessageModel($"Выполнение измерений"), IsBlockStart: true);

      foreach (var points in pointsList)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        var chain = new ChainModel(points);

        string pointStr = string.Empty;
        var str = _basePoint.ToString();
        foreach (var point in points)
        {
          str += $"{EquipmentService.GetPointKey(point)},";
        }
        str = str.Remove(str.Length - 1);
        str += "*";

        await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(str), IsBlockStart: true);
        await DeviceManager.ConnectChainToBusAAsync(chain, messageService);

        var measured = await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken());
        if (!measured.Result)
        {
          errorChain.Add(new List<ChainModel>() { _basePoint, chain });
        }

        await DeviceManager.DisconnectChainFromBusAAsync(chain, messageService);
      }

      if (errorChain.Count > 0)
      {
        foreach (var chain in errorChain)
        {
          var chainStr = await PointFormater.GetFormatDisconnectPoint(chain);
          var err = new ShowMessageModel($"{chainStr}", message: "Обнаружено замыкание", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 };

          errorsMessgae.Add(err);

          manager.AddErrorMethod(baseCommandModel.PointErrors.ChainError($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", chainStr, baseCommandModel.StartLineNumber, baseCommandModel.FormattedStartLineNumber));
          await messageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {err.ToString()}"));
        }
      }

      return errorsMessgae;
    }
  }
}


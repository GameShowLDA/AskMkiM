using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Text;
using static Ask.Core.Shared.Metadata.Static.DelegateManager;
using static Ask.LogLib.LoggerUtility;

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
    /// Асинхронно выполняет проверку соединённых точек в схеме, формируя новый список цепей (ССИРТ)
    /// с учётом обнаруженных разрывов.
    /// </summary>
    static public async Task<(List<ShowMessageModel> errorMessage, List<ShowMessageModel> infoMessage)> CheckSequenceAsync(ConnectedPointContext context, PreMeasurementDelegate preMeasurementDelegate = null)
    {
      var errors = new List<ShowMessageModel>();
      var infos = new List<ShowMessageModel>();
      var sourceGroups = context.SchemeModel?.GroupModels ?? new List<GroupModel>();
      if (sourceGroups.Count == 0)
      {
        return (errors, infos);
      }

      if (context.TypeCommand != MeasurementTypeCommand.KC)
      {
        await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.MessageRelativeToFirstPoint, context.IsPolarityReversed));
      }
      else
      {
        await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.ResistanceRelativeToFirstPoint, context.IsPolarityReversed));
      }

      var newGroups = new List<GroupModel>();

      foreach (var group in sourceGroups)
      {
        var newGroup = new GroupModel();

        foreach (var chain in group.ChainModels)
        {
          var chainCopy = CloneChain(chain);
          if (chainCopy.PointModels.Count == 0)
          {
            continue;
          }

          LogDebug($"[ConnectedPointChecker] Start chain check. Points={chainCopy.PointModels.Count}. Chain={BuildChainDisplayString(chainCopy)}");

          await context.MessageService.AppendEmptyLineAsync();
          await context.MessageService.ShowMessageAsync(
            ExecutorMessageBuilder.BuildChainCheckBlock(BuildChainDisplayString(chainCopy)),
            IsBlockStart: true);

          var result = await ProcessChainAsync(chainCopy.PointModels, context, indentLevel: 1, preMeasurementDelegate);

          LogDebug($"[ConnectedPointChecker] Chain checked. Fragments={result.Fragments.Count}. Display={BuildDisconnectionDisplayString(result.Fragments)}");

          newGroup.ChainModels.AddRange(result.Fragments);
          errors.AddRange(result.Errors);
          infos.AddRange(result.Infos);

          if (context.TypeCommand == MeasurementTypeCommand.KC)
          {
            foreach (var failedMeasurement in result.FailedMeasurements)
            {
              var error = ExecutorMessageBuilder.BuildMeasurementResultMessage(
                MeasurementTypeCommand.KC,
                context.LowerLimit,
                context.HigherLimit,
                failedMeasurement.Value,
                chains: $"{failedMeasurement.Chain} ");
              error.Status = ShowMessageModel.MessageType.Error;
              error.IndentLevel = 2;

              errors.Add(error);

              context.CommandManager.AddErrorMethod(
                context.CommandModel.PointErrors.DisconnectChainError(
                  $"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}",
                  error.Header,
                  error.Message,
                  context.CommandModel.StartLineNumber,
                  context.CommandModel.FormattedStartLineNumber));

              await context.MessageService.ShowMessageAsync(error);
            }
          }
          // Добавляем одну запись об ошибке на исходную цепь, если есть разрывы (фрагментов > 1).
          else if (result.Fragments.Count > 1)
          {
            var chainStr = BuildDisconnectionDisplayString(result.Fragments);

            ShowMessageModel error;
            string valueForProtocol;

            if (context.TypeCommand == MeasurementTypeCommand.PR)
            {
              error = new ShowMessageModel(chainStr, type: ShowMessageModel.MessageType.Error)
              {
                IndentLevel = 2
              };
              valueForProtocol = string.Empty;
            }
            else
            {
              var value = result.FirstFailureValue ?? 0;
              error = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, value, chainStr);
              error.Status = ShowMessageModel.MessageType.Error;
              error.IndentLevel = 2;
              valueForProtocol = $"{value} {(string.IsNullOrEmpty(context.Unit) ? "Ом" : context.Unit)}";
            }

            errors.Add(error);
            await context.MessageService.ShowMessageAsync(error);

            context.CommandManager.AddErrorMethod(
              context.CommandModel.PointErrors.DisconnectChainError(
                $"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}",
                chainStr,
                valueForProtocol,
                context.CommandModel.StartLineNumber,
                context.CommandModel.FormattedStartLineNumber));
          }
        }

        if (newGroup.ChainModels.Count > 0)
        {
          newGroups.Add(newGroup);
        }
      }

      var updatedScheme = new SchemeModel(newGroups);
      context.NewScheme = updatedScheme;

      return (errors, infos);
    }

    /// <summary>
    /// Рекурсивная проверка цепи: первая точка подключается к нижней шине,
    /// остальные по очереди к верхней шине с тестом на связь.
    /// </summary>
    private static async Task<ChainProcessingResult> ProcessChainAsync(List<PointModel> points, ConnectedPointContext context, int indentLevel, PreMeasurementDelegate preMeasurementDelegate = null)
    {
      var result = new ChainProcessingResult();

      if (points == null || points.Count == 0)
      {
        return result;
      }

      if (points.Count == 1)
      {
        result.Fragments.Add(new ChainModel(new List<PointModel>(points)));
        return result;
      }

      var basePoint = points[0];
      var connectedPoints = new List<PointModel> { basePoint };
      var brokenPoints = new List<PointModel>();
      double? firstFailureValue = null;

      var messageService = context.MessageService;
      bool baseConnected = false;

      LogDebug($"[ConnectedPointChecker] Enter fragment. Count={points.Count}, Base={basePoint.Mnemonic}, Indent={indentLevel}");

      await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек") { IndentLevel = indentLevel }, IsBlockStart: true);
      await DeviceManager.RelayModule.PointManager.ConnectPointToBusBAsync(basePoint, messageService, false);
      baseConnected = true;

      if (preMeasurementDelegate != null)
      {
        await preMeasurementDelegate(messageService.GetCancellationToken());
      }

      try
      {
        foreach (var point in points.Skip(1))
        {
          messageService.GetCancellationToken().ThrowIfCancellationRequested();

          await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildPointsCheckHeaderAsync(basePoint, point, CircuitFaultType.ShortCircuit), IsBlockStart: true);

          var pointConnected = false;
          await DeviceManager.RelayModule.PointManager.ConnectPointToBusAAsync(point, messageService, false);
          pointConnected = true;

          try
          {
            var module = EquipmentService.GetModuleByPoint(point);

            (bool Result, double Value) measured;

            if (context.TypeCommand == MeasurementTypeCommand.IE)
            {
              measured = await context.PerformMeasurementAsync(context.Value, messageService, messageService.GetCancellationToken(), module.SwitchCapacitance);
            }
            else
            {
              measured = await context.PerformMeasurementAsync(context.Value, messageService, messageService.GetCancellationToken(), module.SwitchResistance);
            }


            var chain = new ChainModel(new List<PointModel> { basePoint, point });
            var chainStr = context.CommandModel.BuildDislpayInfo.BuildErrorChainStringAsync(chain);

            LogDebug($"[ConnectedPointChecker] Test {basePoint.Mnemonic}->{point.Mnemonic}. Result={(measured.Result ? "OK" : "FAIL")} Value={measured.Value}");

            if (!measured.Result)
            {
              brokenPoints.Add(point);
              firstFailureValue ??= measured.Value;

              if (context.TypeCommand == MeasurementTypeCommand.KC)
              {
                result.FailedMeasurements.Add(new FailedMeasurement(chainStr, measured.Value));
              }
            }
            else
            {
              connectedPoints.Add(point);
            }

            if (context.IsProtocolAttribute)
            //if (context.IsProtocolAttribute && measured.Result)
            {
              var info = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, measured.Value, chainStr);
              info.IndentLevel = indentLevel + 1;
              result.Infos.Add(info);
            }
          }
          finally
          {
            if (pointConnected)
            {
              await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(point, messageService, false);
            }
          }
        }
      }
      finally
      {
        if (baseConnected)
        {
          await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(basePoint, messageService, false);
        }
      }

      // Фрагмент без разрывов (базовая точка + все успешно проверенные).
      var connectedFragment = new ChainModel(new List<PointModel>(connectedPoints));
      result.Fragments.Add(connectedFragment);
      result.FirstFailureValue ??= firstFailureValue;

      // Если найдены разрывы, формируем новую цепь из точек с разрывами и повторяем проверку.
      if (brokenPoints.Count > 0 && context.TypeCommand != MeasurementTypeCommand.KC)
      {
        var nextFragment = await ProcessChainAsync(brokenPoints, context, indentLevel + 1, preMeasurementDelegate);
        result.Append(nextFragment);
      }
      else
      {
        LogDebug($"[ConnectedPointChecker] Fragment is intact. Count={connectedFragment.PointModels.Count}");
      }

      return result;
    }

    private static string BuildChainDisplayString(ChainModel chain)
    {
      var builder = new StringBuilder("*");

      for (int i = 0; i < chain.PointModels.Count; i++)
      {
        var point = chain.PointModels[i];
        var address = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{point.ToString()}]" : string.Empty;
        var delimiter = (i + 1) == chain.PointModels.Count ? "*" : ",";
        builder.Append($"{point.Mnemonic}{address}{delimiter}");
      }

      return builder.ToString();
    }

    private static ChainModel CloneChain(ChainModel source)
    {
      var clone = new ChainModel();
      clone.PointModels.AddRange(source.PointModels);
      return clone;
    }

    private static string BuildDisconnectionDisplayString(List<ChainModel> fragments)
    {
      var fragmentStrings = fragments.Select(fragment =>
      {
        var points = fragment.PointModels.Select(p =>
        {
          var address = DeviceDisplayConfig.GetMachineAddressVisibility() ? $" [{p.ToString()}]" : string.Empty;
          return $"{p.Mnemonic}{address}";
        });

        return string.Join(",", points);
      });

      return $"*{string.Join("**", fragmentStrings)}*";
    }

    private sealed class FailedMeasurement
    {
      public FailedMeasurement(string chain, double value)
      {
        Chain = chain;
        Value = value;
      }

      public string Chain { get; }
      public double Value { get; }
    }

    private sealed class ChainProcessingResult
    {
      public List<ChainModel> Fragments { get; } = new();
      public List<ShowMessageModel> Errors { get; } = new();
      public List<ShowMessageModel> Infos { get; } = new();
      public List<FailedMeasurement> FailedMeasurements { get; } = new();
      public double? FirstFailureValue { get; set; }

      public void Append(ChainProcessingResult other)
      {
        if (other == null)
          return;

        Fragments.AddRange(other.Fragments);
        Errors.AddRange(other.Errors);
        Infos.AddRange(other.Infos);
        FailedMeasurements.AddRange(other.FailedMeasurements);
        FirstFailureValue ??= other.FirstFailureValue;
      }
    }
  }
}

using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Linq;
using System.Text;

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
    static public async Task<(List<ShowMessageModel> errorMessage, List<ShowMessageModel> infoMessage)> CheckSequenceAsync(ConnectedPointContext context)
    {
      var errors = new List<ShowMessageModel>();
      var infos = new List<ShowMessageModel>();

      var sourceGroups = context.SchemeModel?.GroupModels ?? new List<GroupModel>();
      if (sourceGroups.Count == 0)
      {
        return (errors, infos);
      }

      await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.MessageRelativeToFirstPoint, context.IsPolarityReversed));

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

          await context.MessageService.AppendEmptyLineAsync();
          await context.MessageService.ShowMessageAsync(
            ExecutorMessageBuilder.BuildChainCheckBlock(BuildChainDisplayString(chainCopy)),
            IsBlockStart: true);

          var result = await ProcessChainAsync(chainCopy.PointModels, context, indentLevel: 1);

          newGroup.ChainModels.AddRange(result.Fragments);
          errors.AddRange(result.Errors);
          infos.AddRange(result.Infos);
        }

        if (newGroup.ChainModels.Count > 0)
        {
          newGroups.Add(newGroup);
        }
      }

      if (errors.Count > 0)
      {
        await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Результаты проверки") { IndentLevel = 1 });
        foreach (var error in errors)
        {
          await context.MessageService.ShowMessageAsync(error);
        }
      }

      // Формируем новый ССИРТ с учётом разрывов.
      var updatedScheme = new SchemeModel(newGroups);
      context.SchemeModel = updatedScheme;
      if (context.CommandModel is IHasScheme hasScheme)
      {
        hasScheme.Scheme = updatedScheme;
      }

      return (errors, infos);
    }

    /// <summary>
    /// Рекурсивная проверка цепи: первая точка подключается к нижней шине,
    /// остальные по очереди к верхней шине с тестом на связь.
    /// </summary>
    private static async Task<ChainProcessingResult> ProcessChainAsync(List<PointModel> points, ConnectedPointContext context, int indentLevel)
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

      var messageService = context.MessageService;
      bool baseConnected = false;

      await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек") { IndentLevel = indentLevel }, IsBlockStart: true);
      await DeviceManager.RelayModule.PointManager.ConnectPointToBusBAsync(basePoint, messageService, false);
      baseConnected = true;

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
            var measured = await context.PerformMeasurementAsync(context.Value, messageService, messageService.GetCancellationToken(), module.SwitchResistance);

            var chain = new ChainModel(new List<PointModel> { basePoint, point });
            var chainStr = context.CommandModel.BuildDislpayInfo.BuildErrorChainStringAsync(chain);

            if (!measured.Result)
            {
              brokenPoints.Add(point);

              var error = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, measured.Value, chainStr);
              error.Status = ShowMessageModel.MessageType.Error;
              error.IndentLevel = indentLevel + 1;
              result.Errors.Add(error);

              context.CommandManager.AddErrorMethod(context.CommandModel.PointErrors.DisconnectChainError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", chainStr, $"{measured.Value} Ом", context.CommandModel.StartLineNumber, context.CommandModel.FormattedStartLineNumber));
            }
            else
            {
              connectedPoints.Add(point);
            }

            if (context.IsProtocolAttribute)
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
      result.Fragments.Add(new ChainModel(new List<PointModel>(connectedPoints)));

      // Если найдены разрывы, формируем новую цепь из точек с разрывами и повторяем проверку.
      if (brokenPoints.Count > 0)
      {
        var nextFragment = await ProcessChainAsync(brokenPoints, context, indentLevel + 1);
        result.Append(nextFragment);
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

    private sealed class ChainProcessingResult
    {
      public List<ChainModel> Fragments { get; } = new();
      public List<ShowMessageModel> Errors { get; } = new();
      public List<ShowMessageModel> Infos { get; } = new();

      public void Append(ChainProcessingResult other)
      {
        if (other == null)
          return;

        Fragments.AddRange(other.Fragments);
        Errors.AddRange(other.Errors);
        Infos.AddRange(other.Infos);
      }
    }
  }
}

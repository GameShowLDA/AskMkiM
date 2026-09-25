using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using System.Windows;
using System.Windows.Media;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует сообщения о выполнении команд программы контроля, проверке цепей, точек и разрядов.
/// </summary>
internal static class CommandMessageBuilder
{
  /// <summary>
  /// Формирует заголовок блока проверки в соответствии с алгоритмом контроля.
  /// </summary>
  /// <param name="algorithm">Алгоритм проверки.</param>
  /// <param name="inversion">Признак инверсии проверки.</param>
  /// <returns>Модель сообщения с заголовком блока проверки.</returns>
  internal static ShowMessageModel BuildCheckBlockHeader(ControlCheckAlgorithm algorithm, bool inversion)
  {
    string header = algorithm.GetDescription();
    if (string.Equals(header, ControlCheckAlgorithm.MessageRelativeToFirstPoint.GetDescription()))
    {
      header = string.Empty;
    }
    if (inversion)
    {
      header += "(инверсия)";
    }

    return new ShowMessageModel
    {
      Header = header,
      Status = ShowMessageModel.MessageType.CommandBlock,
    };
  }

  /// <summary>
  /// Формирует сообщение о выполнении команды программы контроля.
  /// </summary>
  /// <param name="commandName">Имя выполняемой команды.</param>
  /// <param name="message">Дополнительное сообщение команды.</param>
  /// <returns>Модель сообщения о выполнении команды.</returns>
  internal static ShowMessageModel BuildCommandExecutionMessage(string commandName, string? message = null)
  {
    var model = new ShowMessageModel(
      header: message ?? string.Empty,
      type: ShowMessageModel.MessageType.Command)
    {
      IsControlProgramCommandHeader = !commandName.Contains("ПИ/", StringComparison.OrdinalIgnoreCase),
    };

    if (model.MessageColor.HasValue)
    {
      model.HeaderColor = model.MessageColor.Value;
      model.HeaderBackgroundColor = UserInterfaceConfig.GetCommandBodyBackgroundHighlighting()
        ? BuildPaleTextBackground(model.MessageColor.Value)
        : null;
    }

    return model;
  }

  /// <summary>
  /// Формирует сообщение о проверке цепи.
  /// </summary>
  /// <param name="chains">Обозначение проверяемых цепей.</param>
  /// <returns>Модель сообщения о проверке цепи.</returns>
  internal static ShowMessageModel BuildChainCheckBlock(string chains)
  {
    var model = new ShowMessageModel(
      header: "Проверка цепи",
      message: chains,
      type: ShowMessageModel.MessageType.CommandBlock)
    {
      IndentLevel = 1,
    };

    ApplyCommandBlockBackground(model);
    return model;
  }

  /// <summary>
  /// Формирует заголовок проверки соединения между двумя точками.
  /// </summary>
  /// <param name="firstPoint">Первая проверяемая точка.</param>
  /// <param name="secondPoint">Вторая проверяемая точка.</param>
  /// <param name="circuitFaultType">Тип неисправности цепи.</param>
  /// <returns>Модель сообщения о проверке точек.</returns>
  internal static ShowMessageModel BuildPointsCheckHeader(
    PointModel firstPoint,
    PointModel secondPoint,
    CircuitFaultType circuitFaultType)
  {
    string firstAddress = string.Empty;
    string secondAddress = string.Empty;

    if (DeviceDisplayConfig.GetMachineAddressVisibility())
    {
      if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
      {
        firstAddress = $"({LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(firstPoint.ToString())})";
        secondAddress = $"({LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(secondPoint.ToString())})";
      }
      else
      {
        firstAddress = $"({firstPoint})";
        secondAddress = $"({secondPoint})";
      }
    }

    char symbol = circuitFaultType == CircuitFaultType.OpenCircuit ? '*' : ',';

    var model = new ShowMessageModel(
      header: "Проверка",
      message: $"{firstPoint.Mnemonic}{firstAddress}{symbol}{secondPoint.Mnemonic}{secondAddress}",
      type: ShowMessageModel.MessageType.CommandBlock)
    {
      IndentLevel = 1,
    };

    ApplyCommandBlockBackground(model);
    return model;
  }

  /// <summary>
  /// Формирует сообщение о проверке разряда.
  /// </summary>
  /// <param name="dischargeNumber">Номер проверяемого разряда.</param>
  /// <param name="dischargeView">Представление проверяемого разряда.</param>
  /// <returns>Модель сообщения о проверке разряда.</returns>
  internal static ShowMessageModel BuildDischargeCheckBlock(int dischargeNumber, string dischargeView)
  {
    return new ShowMessageModel(
      header: $"Проверка разряда {dischargeNumber}",
      message: dischargeView,
      type: ShowMessageModel.MessageType.CommandBlock)
    {
      IndentLevel = 1,
    };
  }

  /// <summary>
  /// Формирует сообщение об ошибке при проверке разряда.
  /// </summary>
  /// <param name="dischargeNumber">Номер проверяемого разряда.</param>
  /// <param name="dischargeView">Представление проверяемого разряда.</param>
  /// <returns>Модель сообщения об ошибке проверки разряда.</returns>
  internal static ShowMessageModel BuildDischargeCheckError(int dischargeNumber, string dischargeView)
  {
    return new ShowMessageModel(
      header: $"Ошибка при проверке разряда {dischargeNumber}",
      message: dischargeView,
      type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 1,
    };
  }

  /// <summary>
  /// Формирует сообщение о проверке диода в заданном направлении.
  /// </summary>
  /// <param name="isDirectDirection">
  /// Признак проверки диода в прямом направлении.
  /// </param>
  /// <returns>Модель сообщения о направлении проверки диода.</returns>
  internal static ShowMessageModel BuildDiodeDirectionMessage(bool isDirectDirection)
  {
    return new ShowMessageModel(
      isDirectDirection
        ? "Проверка диода в прямом направлении:"
        : "Проверка диода в обратном направлении:")
    {
      IndentLevel = 1,
    };
  }

  /// <summary>
  /// Формирует сообщение о подключении точек.
  /// </summary>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <returns>Модель сообщения о подключении точек.</returns>
  internal static ShowMessageModel BuildPointsConnectionMessage(int indentLevel)
    => new("Подключение точек") { IndentLevel = indentLevel };

  /// <summary>
  /// Формирует сообщение о срабатывании точки останова на команде.
  /// </summary>
  /// <param name="commandName">Имя команды, на которой сработала точка останова.</param>
  /// <param name="commandBody">Тело команды.</param>
  /// <returns>Модель сообщения о срабатывании точки останова.</returns>
  internal static ShowMessageModel BuildBreakpointHitMessage(
    string commandName,
    string commandBody)
  {
    return new ShowMessageModel(
      header: $"\r\nСработала точка останова на команде {commandName}",
      headerColor: ShowMessageModel.SuccessMessage.TitleColor,
      message: commandBody,
      type: ShowMessageModel.MessageType.Command)
    {
      IndentLevel = 1,
    };
  }

  /// <summary>
  /// Формирует сообщение о переходе к указанной команде.
  /// </summary>
  /// <param name="commandName">Имя команды, к которой выполняется переход.</param>
  /// <param name="commandBody">Тело команды.</param>
  /// <returns>Модель сообщения о переходе к команде.</returns>
  internal static ShowMessageModel BuildCommandJumpMessage(
    string commandName,
    string commandBody)
  {
    return new ShowMessageModel(
      header: $"\r\nПереход к команде {commandName}",
      message: commandBody,
      type: ShowMessageModel.MessageType.Command)
    {
      IndentLevel = 1,
    };
  }

  /// <summary>
  /// Формирует сообщение о начале выполнения программы контроля.
  /// </summary>
  /// <param name="objectName">Наименование контролируемого объекта.</param>
  /// <param name="objectCode">Код контролируемого объекта.</param>
  /// <returns>Модель сообщения о начале выполнения программы контроля.</returns>
  internal static ShowMessageModel BuildControlProgramStartMessage(
    string objectName,
    string objectCode)
  {
    return new ShowMessageModel(
      $"Протокол выполнения программы контроля для \"{objectName}({objectCode})\"",
      type: ShowMessageModel.MessageType.Command)
    {
      IsControlProgramCommandHeader = true,
    };
  }

  /// <summary>
  /// Применяет фон блока команды к сообщению в соответствии с настройками интерфейса.
  /// </summary>
  /// <param name="model">Модель сообщения, для которой настраивается фон.</param>
  private static void ApplyCommandBlockBackground(ShowMessageModel model)
  {
    if (!UserInterfaceConfig.GetChainPointBodyBackgroundHighlighting())
    {
      model.HeaderBackgroundColor = null;
      return;
    }

    Color? commandBlockColor = TryGetResourceColor("LightBlueColorSolidColorBrush");
    if (commandBlockColor.HasValue)
    {
      model.HeaderBackgroundColor = BuildPaleTextBackground(commandBlockColor.Value);
    }
  }

  /// <summary>
  /// Формирует полупрозрачный цвет фона на основе цвета текста.
  /// </summary>
  /// <param name="textColor">Исходный цвет текста.</param>
  /// <returns>Цвет фона с уменьшенной прозрачностью.</returns>
  private static Color BuildPaleTextBackground(Color textColor)
  {
    const byte paleAlpha = 70;
    return Color.FromArgb(paleAlpha, textColor.R, textColor.G, textColor.B);
  }

  /// <summary>
  /// Получает цвет ресурса интерфейса по указанному ключу.
  /// </summary>
  /// <param name="resourceKey">Ключ ресурса интерфейса.</param>
  /// <returns>
  /// Цвет найденного ресурса или <see langword="null"/>, если ресурс не найден.
  /// </returns>
  private static Color? TryGetResourceColor(string resourceKey)
  {
    Color? color = null;
    try
    {
      if (Application.Current != null)
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          if (Application.Current?.Resources[resourceKey] is SolidColorBrush brush)
          {
            color = brush.Color;
          }
        });
      }
    }
    catch (Exception)
    {
    }

    return color;
  }
}

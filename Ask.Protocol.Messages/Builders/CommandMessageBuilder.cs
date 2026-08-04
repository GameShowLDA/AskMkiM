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
  internal static ShowMessageModel BuildCheckBlockHeader(ControlCheckAlgorithm algorithm, bool inversion)
  {
    string header = algorithm.GetDescription();
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

  private static Color BuildPaleTextBackground(Color textColor)
  {
    const byte paleAlpha = 70;
    return Color.FromArgb(paleAlpha, textColor.R, textColor.G, textColor.B);
  }

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

using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using System;
using System.Reflection;
using System.Windows.Media;

namespace Ask.Core.Shared.Metadata.Static.Messages
{
  /// <summary>
  /// Формирует сообщения, используемые в процессе выполнения программ контроля.
  /// </summary>
  public static class ExecutorMessageBuilder
  {
    /// <summary>
    /// Создаёт заголовок блока проверки по алгоритму контроля.
    /// </summary>
    public static ShowMessageModel BuildCheckBlockHeader(ControlCheckAlgorithm algorithm, bool inversion)
    {
      string header = algorithm.GetDescription();
      if (inversion)
      {
        header += "(инверсия)";
      }

      return new ShowMessageModel
      {
        Header = header,
        Status = ShowMessageModel.MessageType.CommandBlock
      };
    }

    /// <summary>
    /// Формирует сообщение-заголовок для выполнения команды.
    /// </summary>
    public static ShowMessageModel BuildCommandExecutionMessage(
        string commandName,
        string? message = null)
    {
      var header = string.Empty;
      if (!string.IsNullOrEmpty(message))
      {
        header += message;
      }

      var model = new ShowMessageModel
      (
          header: header,
          type: ShowMessageModel.MessageType.Command
      )
      {
        IndentLevel = 1,
        IsControlProgramCommandHeader = !commandName.Contains("ПИ/", StringComparison.OrdinalIgnoreCase)
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
    /// Формирует бледный фон для текста на основе его цвета.
    /// </summary>
    private static Color BuildPaleTextBackground(Color textColor)
    {
      const byte paleAlpha = 70; // ~27% непрозрачности
      return Color.FromArgb(paleAlpha, textColor.R, textColor.G, textColor.B);
    }

    /// <summary>
    /// Формирует информационное сообщение о подготовке устройств.
    /// </summary>
    public static ShowMessageModel BuildDevicesPreparationMessage()
    {
      return new ShowMessageModel
      (
          header: "Подготовка устройств",
          type: ShowMessageModel.MessageType.Info
      );
    }

    /// <summary>
    /// Формирует информационное сообщение о настройке мультиметра.
    /// </summary>
    public static ShowMessageModel BuildMultimeterSetupMessage()
    {
      return new ShowMessageModel
      (
          header: "Настройка мультиметра",
          type: ShowMessageModel.MessageType.Info
      );
    }

    /// <summary>
    /// Формирует информационное сообщение о настройке пробойной установки.
    /// </summary>
    public static ShowMessageModel BuildBreakdownTesterSetupMessage()
    {
      return new ShowMessageModel
      (
          header: "Настройка пробойной установки",
          type: ShowMessageModel.MessageType.Info
      );
    }

    /// <summary>
    /// Формирует сообщение-заголовок блока проверки цепей.
    /// </summary>
    public static ShowMessageModel BuildChainCheckBlock(string chains)
    {
      return new ShowMessageModel
      (
          header: "Проверка цепи",
          message: chains,
          type: ShowMessageModel.MessageType.CommandBlock
      )
      {
        IndentLevel = 1
      };
    }

    /// <summary>
    /// Формирует заголовок блока проверки между двумя точками.
    /// </summary>
    public static ShowMessageModel BuildPointsCheckHeaderAsync(PointModel firstPoint, PointModel secondPoint, CircuitFaultType circuitFaultType)
    {
      bool showAddress = DeviceDisplayConfig.GetMachineAddressVisibility();

      string firstAddress = showAddress ? $"({firstPoint})" : string.Empty;
      string secondAddress = showAddress ? $"({secondPoint})" : string.Empty;
      char symbol = circuitFaultType == CircuitFaultType.OpenCircuit ? '*' : ',';

      return new ShowMessageModel
      (
          header: $"Проверка",
          message: $"{firstPoint.Mnemonic}{firstAddress}{symbol}{secondPoint.Mnemonic}{secondAddress}",
          type: ShowMessageModel.MessageType.CommandBlock
      )
      {
        IndentLevel = 1
      };
    }

    /// <summary>
    /// Формирует сообщение-заголовок блока проверки разряда.
    /// </summary>
    public static ShowMessageModel BuildDischargeCheckBlock(string dischargeView)
    {
      return new ShowMessageModel
      (
          header: $"Проверка разряда",
          message: dischargeView,
          type: ShowMessageModel.MessageType.CommandBlock
      )
      {
        IndentLevel = 1
      };
    }

    /// <summary>
    /// Формирует сообщение об ошибке при проверке разряда.
    /// </summary>
    public static ShowMessageModel BuildDischargeCheckError(string dischargeName)
    {
      return new ShowMessageModel
      (
          header: $"Ошибка при проверке разряда {dischargeName}",
          type: ShowMessageModel.MessageType.Error
      )
      {
        IndentLevel = 1
      };
    }

    public static ShowMessageModel BuildDeviceHealthCheckTitle(IAttachableDevice device)
    {
      if (device == null)
        throw new ArgumentNullException(nameof(device));

      return new ShowMessageModel
      (
        header: $"Тест контроля работоспособности",
        message: $"{device.Name} {device.NumberChassis}.{device.Number}",
        type: ShowMessageModel.MessageType.CommandBlock
      );
    }

    public static ShowMessageModel BuildMeasurementResultMessage(MeasurementTypeCommand measurementTypeCommand, double lowerLimit, double higherLimit, double value, string? chains = null, string comparisonSign = "=")
    {
      var type = typeof(MeasurementTypeCommand);

      var member = type
          .GetMember(measurementTypeCommand.ToString())
          .FirstOrDefault();

      var attr = member?
          .GetCustomAttribute<CommandDisplayInfoAttribute>();

      if (attr == null)
      {
        return new ShowMessageModel("Ошибка формирования сообщения измерения", message: "Атрибут CommandDisplayInfoAttribute не найден.");
      }

      if (chains == null)
      {
        chains = string.Empty;
      }

      if (higherLimit != -1)
      {
        if ((value < lowerLimit || value > higherLimit) && (measurementTypeCommand == MeasurementTypeCommand.PI_ACW || measurementTypeCommand == MeasurementTypeCommand.PI_DCW))
        {
          return new ShowMessageModel($"{chains}({lowerLimit}-{higherLimit} {attr.Unit})", message: $"{attr.Symbol.ToString()}изм{comparisonSign} ПРОБОЙ");
        }

        if (value.ToString() == "9,9E+37" && (measurementTypeCommand == MeasurementTypeCommand.EHT || measurementTypeCommand == MeasurementTypeCommand.KC || measurementTypeCommand == MeasurementTypeCommand.PR))
        {
          return new ShowMessageModel($"{chains}({lowerLimit}-{higherLimit} {attr.Unit})", message: $"{attr.Symbol.ToString()}изм{comparisonSign} Overload");
        }

        return new ShowMessageModel($"{chains}({lowerLimit}-{higherLimit} {attr.Unit})", message: $"{attr.Symbol.ToString()}изм{comparisonSign} {value} {attr.Unit}");
      }
      else
      {
        if ((value < lowerLimit || value > higherLimit) && (measurementTypeCommand == MeasurementTypeCommand.PI_ACW || measurementTypeCommand == MeasurementTypeCommand.PI_DCW))
        {
          return new ShowMessageModel($"{chains}({lowerLimit}<{attr.Unit})", message: $"{attr.Symbol.ToString()}изм{comparisonSign} ПРОБОЙ");
        }

        if (value.ToString() == "9,9E+37" && (measurementTypeCommand == MeasurementTypeCommand.EHT || measurementTypeCommand == MeasurementTypeCommand.KC || measurementTypeCommand == MeasurementTypeCommand.PR))
        {
          return new ShowMessageModel($"{chains}({lowerLimit}<{attr.Unit})", message: $"{attr.Symbol.ToString()}изм{comparisonSign} Overload");
        }

        return new ShowMessageModel($"{chains}({lowerLimit}<{attr.Unit})", message: $"{attr.Symbol.ToString()}изм{comparisonSign} {value} {attr.Unit}");
      }
    }
  }
}

using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует сообщения о подготовке, запуске, этапах и завершении выполняемого процесса.
/// </summary>
internal static class ExecutionMessageBuilder
{
  internal static ShowMessageModel BuildDevicesPreparationMessage()
    => new(header: "Подготовка устройств", type: ShowMessageModel.MessageType.Info);

  internal static ShowMessageModel BuildMultimeterSetupMessage()
    => new(header: "Настройка мультиметра", type: ShowMessageModel.MessageType.Info);

  internal static ShowMessageModel BuildBreakdownTesterSetupMessage()
    => new(header: "Настройка пробойной установки", type: ShowMessageModel.MessageType.Info);

  internal static ShowMessageModel BuildEquipmentInitializationMessage()
    => new(header: "Инициализация оборудования");

  internal static ShowMessageModel BuildEquipmentSetupMessage()
    => new(header: "Настройка оборудования");

  internal static ShowMessageModel BuildTestStartedMessage()
    => new(header: "Инициализация завершена, тест начат!");

  internal static ShowMessageModel BuildTestStageMessage(string title)
    => new(header: title);

  internal static ShowMessageModel BuildTestPointMessage(int pointNumber)
    => new(header: $"Тест точки {pointNumber}");

  internal static ShowMessageModel BuildOperationResultMessage(
    bool isSuccessful,
    string successHeader,
    string successMessage,
    string errorHeader,
    string errorMessage)
  {
    return new ShowMessageModel(
      isSuccessful ? successHeader : errorHeader,
      message: isSuccessful ? successMessage : errorMessage,
      type: isSuccessful
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 2,
    };
  }

  internal static ShowMessageModel BuildErrorMessage(string details)
    => new("Ошибка", message: details, type: ShowMessageModel.MessageType.Error);

  internal static ShowMessageModel BuildDevicesInitializationMessage()
    => new("Инициализация устройств", type: ShowMessageModel.MessageType.Info);

  internal static ShowMessageModel BuildMeasurementDeviceSetupMessage()
    => new("Настройка измерителя", type: ShowMessageModel.MessageType.Info);

  internal static ShowMessageModel BuildBusConnectionMessage()
    => new("Подключение шин");

  internal static ShowMessageModel BuildPointConnectionMessage()
    => new("Подключение точек");

  internal static ShowMessageModel BuildPointsResetMessage()
    => new("Сброс точек") { IndentLevel = 1 };

  internal static ShowMessageModel BuildDelayBeforeEnablingMessage(double? seconds)
  {
    return new ShowMessageModel(
      "Задержка перед включением",
      message: $"{seconds}сек.")
    {
      IndentLevel = 2,
    };
  }

  internal static ShowMessageModel BuildDelayBeforeDisablingMessage(double? seconds)
  {
    return new ShowMessageModel(
      "Задержка перед отключением",
      message: $"{seconds}сек.")
    {
      IndentLevel = 2,
    };
  }

  internal static ShowMessageModel BuildDebugMessage(string details)
    => new(debug: details);
}

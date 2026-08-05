using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;

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

  internal static ShowMessageModel BuildPointConnectionMessage(PointModel point)
    => new($"Подлючение точки {point}");

  internal static ShowMessageModel BuildPointDisconnectionMessage(PointModel point)
    => new($"Отлючение точки {point}");

  internal static ShowMessageModel BuildPointsDisconnectionMessage()
    => new("Отлючение точек");

  internal static ShowMessageModel BuildPointsResetMessage()
    => new("Сброс точек") { IndentLevel = 1 };

  internal static ShowMessageModel BuildGeneralPointsResetMessage()
    => new("\tОбщий сброс точек");

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

  internal static ShowMessageModel BuildCheckResultsHeader()
    => new("Результаты проверки") { IndentLevel = 1 };

  internal static ShowMessageModel BuildChainInspectionMessage(string chain)
    => new($"Проверка {chain}");

  internal static ShowMessageModel BuildDefectivePointsMessage()
    => new("Бракованные точки");

  internal static ShowMessageModel BuildDefectiveChainMessage(string chain)
    => new("Найден брак при проверке цепи", message: chain, type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 1,
    };

  internal static ShowMessageModel BuildShortCircuitAnalysisMessage()
    => new("Анализ на наличие короткого замыкания между точками");

  internal static ShowMessageModel BuildLocalizationStepMessage(int step)
    => new($"Выполнение шага {step}");

  internal static ShowMessageModel BuildGroupPartOperationMessage(string operation)
    => new(operation);

  internal static ShowMessageModel BuildLocalizationFailureMessage()
    => new(
      "Локализация не удалась",
      message: "Не удалось точно определить неисправную цепь",
      type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 3,
    };

  internal static ShowMessageModel BuildLocalizationErrorMessage()
    => new(
      "Ошибка локализации",
      message: "Не удалось точно определить замыкание цепей",
      type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 3,
    };

  internal static ShowMessageModel BuildUnknownCommandMessage(string mnemonic)
    => new("Неизвестная команда", message: mnemonic, type: ShowMessageModel.MessageType.Error);

  internal static ShowMessageModel BuildEmergencyExecutionMessage(
    string commandName,
    string details)
  {
    return new ShowMessageModel(
      "\r\nОшибка выполнения команды",
      message: $"Команда: {commandName}. {details} Запускается аварийное выполнение КЦ.",
      type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 3,
    };
  }

  internal static ShowMessageModel BuildEmergencyKscErrorMessage(string details)
    => new("Ошибка аварийного выполнения КЦ", message: details, type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 3,
    };

  internal static ShowMessageModel BuildModuleBusConnectionMessage(string moduleName, int moduleNumber)
    => new($"{moduleName}({moduleNumber})", message: "Подключение к шинам A1B1", type: ShowMessageModel.MessageType.Info);

  internal static ShowMessageModel BuildPointRangeConnectionMessage(
    int chassisNumber,
    int moduleNumber,
    int startPoint,
    int endPoint)
    => new(
      $"{chassisNumber}.{moduleNumber}.{startPoint} - {endPoint}",
      message: "Подключение точек к шинам",
      type: ShowMessageModel.MessageType.Info);

}

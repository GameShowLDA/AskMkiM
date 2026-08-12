using Ask.Core.Shared.DTO.Settings;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Класс конфигурации для <see cref="ProtocolConfig"/>.
  /// </summary>
  public static class ProtocolConfig
  {
    public static Action<SettingsProtocolDto>? SaveProtocolEvent;
    public static Action<bool>? TestStepMessagesInProtocolChanged;
    public static Func<SettingsProtocolDto, Task>? SaveProtocolAsyncEvent;

    private static SettingsProtocolDto ProtocolModel = new SettingsProtocolDto();

    #region Set.

    /// <summary>
    /// Устанавливает отображение информации об устройствах в протоколе.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static void SetDeviceInfo(bool enable) => ProtocolModel.ShowDeviceInfo = enable;
    /// <summary>
    /// Устанавливает отображение информации об устройствах в протоколе.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static void SetHeaderInfo(bool enable) => ProtocolModel.ShowHeaderInfo = enable;

    /// <summary>
    /// Устанавливает режим подробного вывода информации в протокол.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    static public void SetShowDetailedProtocol(bool enable) => ProtocolModel.ShowDetailedProtocol = enable;

    /// <summary>
    /// Устанавливает автосохранение протокола.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static void SetSaveProtocol(bool enable) => ProtocolModel.AutoSaveProtocol = true;

    /// <summary>
    /// Устанавливает автоматическую печать протокола.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static void SetPrintProtocol(bool enable) => ProtocolModel.AutoPrintProtocol = enable;
    /// <summary>
    /// Управляет формированием итогового протокола программы контроля.
    /// </summary>
    /// <param name="enable"><see langword="true"/> для формирования; иначе — <see langword="false"/>.</param>
    public static void SetTimeStart(bool enable) => ProtocolModel.DisplayOperationTime = enable;

    /// <summary>
    /// Устанавливает отображение времени выполнения операций.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static void SetShowProtocolInSoftware(bool enable) => ProtocolModel.ShowProtocolInSoftware = enable;

    /// <summary>
    /// Устанавливает отображение времени выполнения операций.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static void SetGenerateProtocol(bool enable) => ProtocolModel.GenerateProtocol = true;
    public static void SetCleanTextProtocol(string text) => ProtocolModel.CleanTextProtocol = text;
    public static void SetCleanTextErrorProtocol(string text) => ProtocolModel.CleanTextErrorsProtocol = text;
    public static void SetErrorTextProtocol(string text) => ProtocolModel.ErrorTextProtocol = text;
    public static void SetProtocolModel(SettingsProtocolDto protocolModel)
    {
      ArgumentNullException.ThrowIfNull(protocolModel);

      ProtocolModel = protocolModel;
      ProtocolModel.AutoSaveProtocol = true;
      ProtocolModel.GenerateProtocol = true;
      Ask.Core.Shared.DTO.Protocol.ProtocolModel.SetTemplate(protocolModel.CleanTextProtocol);
      Ask.Core.Shared.DTO.Protocol.ProtocolModel.SetErrorsTemplate(protocolModel.CleanTextErrorsProtocol);
    }

    public static void SetCommandHeadersInProtocol(bool enable) => ProtocolModel.ShowCommandHeadersInProtocol = enable;
    public static void SetTestStepMessagesInProtocol(bool enable) => ProtocolModel.ShowTestStepMessagesInProtocol = enable;
    public static void SetPrintFontFamily(string fontFamily) => ProtocolModel.PrintFontFamily = fontFamily;
    public static void SetPrintFontSize(double fontSize) => ProtocolModel.PrintFontSize = fontSize;

    #endregion

    #region Get.

    /// <summary>
    /// Возвращает статус отображения информации об устройствах в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static bool GetDeviceInfo() => ProtocolModel.ShowDeviceInfo;

    /// <summary>
    /// Возвращает статус отоображения заголовков.
    /// </summary>
    /// <returns></returns>
    public static bool GetHeaderInfo() => ProtocolModel.ShowHeaderInfo;

    /// <summary>
    /// Возвращает статус отображения подробной информации в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static bool GetShowDetailedProtocol() => ProtocolModel.ShowDetailedProtocol;

    /// <summary>
    /// Возвращает статус автосохранения протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static bool GetSaveProtocol() => true;

    /// <summary>
    /// Возвращает статус авто печати протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static bool GetPrintProtocol() => ProtocolModel.AutoPrintProtocol;

    /// <summary>
    /// Определяет, требуется ли запрашивать данные итогового протокола.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если включена печать или формирование протокола.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    public static bool ShouldShowProtocolInfoDialog() =>
      ProtocolModel.AutoPrintProtocol || ProtocolModel.ShowProtocolInSoftware;

    /// <summary>
    /// Возвращает статус отображения времени в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static bool GetTimeStart() => ProtocolModel.DisplayOperationTime;
    public static bool GetShowProtocolInSoftware() => ProtocolModel.ShowProtocolInSoftware;
    public static bool GetGenerateProtocol() => true;
    public static bool GetCommandHeadersInProtocol() => ProtocolModel.ShowCommandHeadersInProtocol;
    public static bool GetTestStepMessagesInProtocol() => ProtocolModel.ShowTestStepMessagesInProtocol;
    public static string GetCleanTextProtocol() => ProtocolModel.CleanTextProtocol;
    public static string GetCleanTextProtocolError() => ProtocolModel.CleanTextErrorsProtocol;
    public static string GetErrorTextProtocol() => ProtocolModel.ErrorTextProtocol;
    public static string GetPrintFontFamily() => ProtocolModel.PrintFontFamily;
    public static double GetPrintFontSize() => ProtocolModel.PrintFontSize;

    public static SettingsProtocolDto GetProtocolModel()
    {
      SettingsProtocolDto protocolModel = new SettingsProtocolDto
      {
        Id = ProtocolModel.Id,
        ShowDeviceInfo = ProtocolModel.ShowDeviceInfo,
        ShowHeaderInfo = ProtocolModel.ShowHeaderInfo,
        ShowDetailedProtocol = ProtocolModel.ShowDetailedProtocol,
        AutoSaveProtocol = ProtocolModel.AutoSaveProtocol,
        AutoPrintProtocol = ProtocolModel.AutoPrintProtocol,
        DisplayOperationTime = ProtocolModel.DisplayOperationTime,
        ShowProtocolInSoftware = ProtocolModel.ShowProtocolInSoftware,
        GenerateProtocol = ProtocolModel.GenerateProtocol,
        CleanTextProtocol = ProtocolModel.CleanTextProtocol,
        CleanTextErrorsProtocol = ProtocolModel.CleanTextErrorsProtocol,
        ErrorTextProtocol = ProtocolModel.ErrorTextProtocol,
        ShowTestStepMessagesInProtocol = ProtocolModel.ShowTestStepMessagesInProtocol,
        ShowCommandHeadersInProtocol = ProtocolModel.ShowCommandHeadersInProtocol,
        PrintFontFamily = ProtocolModel.PrintFontFamily,
        PrintFontSize = ProtocolModel.PrintFontSize
      };
      return protocolModel;
    }

    #endregion

    public static async Task SaveProtocolModel(SettingsProtocolDto protocolModel)
    {
      protocolModel.AutoSaveProtocol = true;
      protocolModel.GenerateProtocol = true;
      bool testStepMessagesChanged = ProtocolModel.ShowTestStepMessagesInProtocol != protocolModel.ShowTestStepMessagesInProtocol;

      ProtocolModel.Id = protocolModel.Id;
      ProtocolModel.ShowDeviceInfo = protocolModel.ShowDeviceInfo;
      ProtocolModel.ShowHeaderInfo = protocolModel.ShowHeaderInfo;
      ProtocolModel.ShowDetailedProtocol = protocolModel.ShowDetailedProtocol;
      ProtocolModel.AutoSaveProtocol = true;
      ProtocolModel.GenerateProtocol = true;
      ProtocolModel.AutoPrintProtocol = protocolModel.AutoPrintProtocol;
      ProtocolModel.DisplayOperationTime = protocolModel.DisplayOperationTime;
      ProtocolModel.ShowProtocolInSoftware = protocolModel.ShowProtocolInSoftware;
      ProtocolModel.GenerateProtocol = protocolModel.GenerateProtocol;
      ProtocolModel.CleanTextProtocol = protocolModel.CleanTextProtocol;
      ProtocolModel.CleanTextErrorsProtocol = protocolModel.CleanTextErrorsProtocol;
      ProtocolModel.ErrorTextProtocol = protocolModel.ErrorTextProtocol;
      ProtocolModel.ShowCommandHeadersInProtocol = protocolModel.ShowCommandHeadersInProtocol;
      ProtocolModel.ShowTestStepMessagesInProtocol = protocolModel.ShowTestStepMessagesInProtocol;
      ProtocolModel.PrintFontFamily = protocolModel.PrintFontFamily;
      ProtocolModel.PrintFontSize = protocolModel.PrintFontSize;

      await InvokeSaveProtocolAsync(protocolModel);
      if (testStepMessagesChanged)
      {
        TestStepMessagesInProtocolChanged?.Invoke(protocolModel.ShowTestStepMessagesInProtocol);
      }

      SaveProtocolEvent?.Invoke(protocolModel);
    }

    private static async Task InvokeSaveProtocolAsync(SettingsProtocolDto protocolModel)
    {
      if (SaveProtocolAsyncEvent == null)
      {
        return;
      }

      foreach (Func<SettingsProtocolDto, Task> handler in SaveProtocolAsyncEvent.GetInvocationList())
      {
        await handler(protocolModel);
      }
    }

    public static string GetBaseTextProtocol() =>
@"Протокол($РЕЖИМ) от $ДАТА
проверки электрических параметров сборочной единицы $ОБОЗНАЧЕНИЕ Зав.N $НОМЕР
Цель проверки: проверка электрических параметров сборочной единицы на соответствие техническим условиям
Оборудование: установка контроля электромонтажа АСК-МКИ-М
Программа проверки: $ПРОГРАММА
  Начало проверки: $НАЧАЛО
  Завершение проверки: $КОНЕЦ
  Время выполнения: $ВРЕМЯ

Обрывов: не обнаружено
Замыканий: не обнаружено
Нарушений изоляции: не обнаружено

Заключение: Изделие $ОБОЗНАЧЕНИЕ Зав.N $НОМЕР
            соответствует требованиям КД

Исполнитель: ________________ / $ИСПОЛНИТЕЛЬ

Представитель ОК: ________________ / $ПРЕДСТАВИТЕЛЬ

Представитель заказчика (ВП): ________________ / $ЗАКАЗЧИК";

    public static string GetBaseTextErrorsProtocol() =>
@"Протокол($РЕЖИМ) от $ДАТА
проверки электрических параметров сборочной единицы $ОБОЗНАЧЕНИЕ Зав.N $НОМЕР
Программа проверки: $ПРОГРАММА

Заключение: Изделие $ОБОЗНАЧЕНИЕ $НАИМЕНОВАНИЕ
            Зав.N $НОМЕР $БРАК(не )соответствует требованиям КД

Исполнитель: ________________ / $ИСПОЛНИТЕЛЬ

Представитель ОТК: ________________ / $ПРЕДСТАВИТЕЛЬ

Представитель заказчика (ВП): ________________ /  $ЗАКАЗЧИК";
  }
}

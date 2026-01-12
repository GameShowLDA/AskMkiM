using Ask.Core.Shared.Entity.Settings;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Класс конфигурации для <see cref="ProtocolConfig"/>.
  /// </summary>
  public static class ProtocolConfig
  {
    static public Action<SettingsProtocolModel> SaveProtocolEvent;

    private static SettingsProtocolModel ProtocolModel = new SettingsProtocolModel();

    #region Set.

    /// <summary>
    /// Устанавливает отображение информации об устройствах в протоколе.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetDeviceInfo(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.ShowDeviceInfo = enable;
      });
    }

    /// <summary>
    /// Устанавливает отображение информации об устройствах в протоколе.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetHeaderInfo(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.ShowHeaderInfo = enable;
      });
    }

    /// <summary>
    /// Устанавливает режим подробного вывода информации в протокол.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    static public async Task SetShowDetailedProtocol(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.ShowDetailedProtocol = enable;
      });
    }

    /// <summary>
    /// Устанавливает автосохранение протокола.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetSaveProtocol(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.AutoSaveProtocol = enable;
      });
    }

    /// <summary>
    /// Устанавливает автоматическую печать протокола.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetPrintProtocol(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.AutoPrintProtocol = enable;
      });
    }

    /// <summary>
    /// Устанавливает отображение времени выполнения операций.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetTimeStart(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.DisplayOperationTime = enable;
      });
    }

    /// <summary>
    /// Устанавливает отображение времени выполнения операций.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetShowProtocolInSoftware(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.ShowProtocolInSoftware = enable;
      });
    }

    /// <summary>
    /// Устанавливает отображение времени выполнения операций.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetGenerateProtocol(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.GenerateProtocol = enable;
      });
    }

    public static async Task SetCleanTextProtocol(string text)
    {
      await Task.Run(() =>
      {
        ProtocolModel.CleanTextProtocol = text;
      });
    }

    public static async Task SetCleanTextErrorProtocol(string text)
    {
      await Task.Run(() =>
      {
        ProtocolModel.CleanTextErrorsProtocol = text;
      });
    }

    public static async Task SetErrorTextProtocol(string text)
    {
      await Task.Run(() =>
      {
        ProtocolModel.ErrorTextProtocol = text;
      });
    }

    public static async Task SetProtocolModel(SettingsProtocolModel protocolModel)
    {
      await Task.Run(() =>
      {
        ProtocolModel = protocolModel;
      });
    }



    #endregion

    #region Get.

    /// <summary>
    /// Возвращает статус отображения информации об устройствах в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetDeviceInfo() => await Task.Run(() => ProtocolModel.ShowDeviceInfo);

    /// <summary>
    /// Возвращает статус отоображения заголовков.
    /// </summary>
    /// <returns></returns>
    public static bool GetHeaderInfo() => ProtocolModel.ShowHeaderInfo;

    /// <summary>
    /// Возвращает статус отображения подробной информации в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetShowDetailedProtocol() => await Task.Run(() => ProtocolModel.ShowDetailedProtocol);

    /// <summary>
    /// Возвращает статус автосохранения протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static async Task<bool> GetSaveProtocol() => await Task.Run(() => ProtocolModel.AutoSaveProtocol);

    /// <summary>
    /// Возвращает статус авто печати протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static async Task<bool> GetPrintProtocol() => await Task.Run(() => ProtocolModel.AutoPrintProtocol);

    /// <summary>
    /// Возвращает статус отображения времени в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetTimeStart() => await Task.Run(() => ProtocolModel.DisplayOperationTime);
    public static async Task<bool> GetShowProtocolInSoftware() => await Task.Run(() => ProtocolModel.ShowProtocolInSoftware);
    public static async Task<bool> GetGenerateProtocol() => await Task.Run(() => ProtocolModel.GenerateProtocol);
    public static async Task<string> GetCleanTextProtocol() => await Task.Run(() => ProtocolModel.CleanTextProtocol);
    public static async Task<string> GetCleanTextProtocolError() => await Task.Run(() => ProtocolModel.CleanTextErrorsProtocol);

    public static async Task<string> GetErrorTextProtocol() => await Task.Run(() => ProtocolModel.ErrorTextProtocol);

    public static async Task<SettingsProtocolModel> GetProtocolModel()
    {
      return await Task.Run(() =>
      {
        SettingsProtocolModel protocolModel = new SettingsProtocolModel();
        protocolModel.ShowDeviceInfo = ProtocolModel.ShowDeviceInfo;
        protocolModel.ShowHeaderInfo = ProtocolModel.ShowHeaderInfo;
        protocolModel.ShowDetailedProtocol = ProtocolModel.ShowDetailedProtocol;
        protocolModel.AutoSaveProtocol = ProtocolModel.AutoSaveProtocol;
        protocolModel.AutoPrintProtocol = ProtocolModel.AutoPrintProtocol;
        protocolModel.DisplayOperationTime = ProtocolModel.DisplayOperationTime;
        protocolModel.ShowProtocolInSoftware = ProtocolModel.ShowProtocolInSoftware;
        protocolModel.GenerateProtocol = ProtocolModel.GenerateProtocol;
        protocolModel.CleanTextProtocol = ProtocolModel.CleanTextProtocol;
        protocolModel.CleanTextErrorsProtocol = ProtocolModel.CleanTextErrorsProtocol;
        return protocolModel;
      });
    }

    #endregion

    public static async Task SaveProtocolModel(SettingsProtocolModel protocolModel)
    {
      await Task.Run(() =>
      {
        ProtocolModel.ShowDeviceInfo = protocolModel.ShowDeviceInfo;
        ProtocolModel.ShowHeaderInfo = protocolModel.ShowHeaderInfo;
        ProtocolModel.ShowDetailedProtocol = protocolModel.ShowDetailedProtocol;
        ProtocolModel.AutoSaveProtocol = protocolModel.AutoSaveProtocol;
        ProtocolModel.AutoPrintProtocol = protocolModel.AutoPrintProtocol;
        ProtocolModel.DisplayOperationTime = protocolModel.DisplayOperationTime;
        ProtocolModel.ShowProtocolInSoftware = protocolModel.ShowProtocolInSoftware;
        ProtocolModel.GenerateProtocol = protocolModel.GenerateProtocol;
        ProtocolModel.CleanTextProtocol = protocolModel.CleanTextProtocol;
        ProtocolModel.CleanTextErrorsProtocol = protocolModel.CleanTextErrorsProtocol;
      });

      SaveProtocolEvent?.Invoke(protocolModel);
    }


    public static async Task<string> GetBaseTextProtocol() => await Task.Run(() =>
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

Представитель заказчика (ВП): ________________ / $ЗАКАЗЧИК");

    public static async Task<string> GetBaseTextErrorsProtocol() => await Task.Run(() =>
@"Протокол($РЕЖИМ) от $ДАТА
проверки электрических параметров сборочной единицы $ОБОЗНАЧЕНИЕ Зав.N $НОМЕР
Программа проверки: $ПРОГРАММА

Заключение: Изделие $ОБОЗНАЧЕНИЕ $НАИМЕНОВАНИЕ
            Зав.N $НОМЕР $БРАК(не )соответствует требованиям КД

Исполнитель: ________________ / $ИСПОЛНИТЕЛЬ

Представитель ОТК: ________________ / $ПРЕДСТАВИТЕЛЬ

Представитель заказчика (ВП): ________________ /  $ЗАКАЗЧИК");
  }
}

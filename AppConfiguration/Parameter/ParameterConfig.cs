using AppConfiguration.Base;
using AppConfiguration.Protocol;

namespace AppConfiguration.Parameter
{
  public static class ParameterConfig
  {
    static ParameterModel ParameterModel = new ParameterModel();


    /// <summary>
    /// Перезаписывает конфигурационный файл протокола с текущими настройками.
    /// </summary>
    public static async void RewriteProtocolConfig()
    {
      ParameterModel protocolModel = new ParameterModel();

      ParameterFileManager protocolFileManager = new ParameterFileManager(FileLocations.ParameterConfigPath);
      await protocolFileManager.RewriteFileAsync(protocolModel);
    }

    #region Set.
    /// <summary>
    /// Устанавливает язык интерйефса программы.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetLanguage(string enable)
    {
      await Task.Run(async () =>
      {
        ParameterModel.Language = enable;
        await LanguageSettings.SetLanguageAsync(enable);
      });
    }
    #endregion

    #region Get.

    /// <summary>
    /// Возвращает язык интерфейса программы.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<string> GetLanguage() => await Task.Run(() => ParameterModel.Language);


    #endregion
  }
}

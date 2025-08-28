using AppConfiguration.Base;
using AppConfiguration.Execution;
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
        var lang = string.IsNullOrWhiteSpace(enable) ? "ru" : enable.ToLowerInvariant();
        ParameterModel.Language = lang;
        await LanguageSettings.SetLanguageAsync(lang);
      });
    }
    #endregion

    #region Get.

    /// <summary>
    /// Возвращает язык интерфейса программы.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<string> GetLanguage() => await Task.Run(() => ParameterModel.Language);

    public static async Task<ParameterModel> GetParameterModel()
    {
      return await Task.Run(() =>
      {
        ParameterModel parametrModel = new ParameterModel();
        parametrModel.Language = ParameterModel.Language;
        return parametrModel;
      });
    }

    public static async Task SaveProtocolModel(ParameterModel protocolModel)
    {
      await Task.Run(() =>
      {
        ParameterModel.Language = protocolModel.Language;
      });

      await RewriteExecutionConfigAsync();
      await LanguageSettings.SetLanguageAsync(ParameterModel.Language);

    }

    /// <summary>
    /// Перезаписывает конфигурацию выполнений.
    /// </summary>
    /// <returns></returns>
    public static async Task RewriteExecutionConfigAsync()
    {
      ParameterModel executionModel = new ParameterModel();
      executionModel.Language = ParameterModel.Language;

      ParameterFileManager executionFileManager = new ParameterFileManager(FileLocations.ParameterConfigPath);
      await executionFileManager.RewriteFileAsync(executionModel);
    }

    #endregion
  }
}

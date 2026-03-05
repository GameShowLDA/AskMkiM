using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.UiEnums;

namespace Ask.Core.Services.Config.Base
{
  public static class UserInterfaceConfig
  {
    private static UserInterfaceModel UserInterfaceModel = new UserInterfaceModel
    {
      Language = "ru"
    };

    static public Action<UserInterfaceModel> SaveUserInterfaceEvent;


    #region Set.
    /// <summary>
    /// Устанавливает язык интерйефса программы.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetLanguage(string enable)
    {
      var lang = LanguageSettings.NormalizeLanguageCode(enable);
      UserInterfaceModel.Language = lang;
      await LanguageSettings.SetLanguageAsync(lang);
    }

    /// <summary>
    /// Устанавливает тему оформления интерфейса программы.
    /// </summary>
    /// <param name="theme">Название темы: "Light" или "Dark".</param>
    public static async Task SetTheme(ThemeMode theme)
    {
      UserInterfaceModel.Theme = theme;
    }

    public static async Task SetSyntaxHighlighting(bool enable)
    {
      UserInterfaceModel.UseSyntaxHighlighting = enable;
    }

    public static async Task SetCommandBodyBackgroundHighlighting(bool enable)
    {
      UserInterfaceModel.UseCommandBodyBackgroundHighlighting = enable;
    }

    public static async Task SetChainPointBodyBackgroundHighlighting(bool enable)
    {
      UserInterfaceModel.UseChainPointBodyBackgroundHighlighting = enable;
    }

    public static async Task SetUserInterfaceModel(UserInterfaceModel user)
    {
      await Task.Run(async () =>
      {
        await SetLanguage(user.Language);
        await SetTheme(user.Theme);
        await SetSyntaxHighlighting(user.UseSyntaxHighlighting);
        await SetCommandBodyBackgroundHighlighting(user.UseCommandBodyBackgroundHighlighting);
        await SetChainPointBodyBackgroundHighlighting(user.UseChainPointBodyBackgroundHighlighting);
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Возвращает язык интерфейса программы.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<string> GetLanguage() => UserInterfaceModel.Language;
    public static async Task<ThemeMode> GetTheme() => UserInterfaceModel.Theme;
    public static bool GetSyntaxHighlighting() => UserInterfaceModel.UseSyntaxHighlighting;
    public static bool GetCommandBodyBackgroundHighlighting() => UserInterfaceModel.UseCommandBodyBackgroundHighlighting;
    public static bool GetChainPointBodyBackgroundHighlighting() => UserInterfaceModel.UseChainPointBodyBackgroundHighlighting;


    public static async Task<UserInterfaceModel> GetParameterModel()
    {
      UserInterfaceModel parametrModel = new UserInterfaceModel();
      parametrModel.Language = UserInterfaceModel.Language;
      parametrModel.Theme = UserInterfaceModel.Theme;
      parametrModel.UseSyntaxHighlighting = UserInterfaceModel.UseSyntaxHighlighting;
      parametrModel.UseCommandBodyBackgroundHighlighting = UserInterfaceModel.UseCommandBodyBackgroundHighlighting;
      parametrModel.UseChainPointBodyBackgroundHighlighting = UserInterfaceModel.UseChainPointBodyBackgroundHighlighting;
      return parametrModel;
    }

    public static async Task SaveProtocolModel(UserInterfaceModel parametrModel)
    {
      await SetLanguage(parametrModel.Language);
      await SetTheme(parametrModel.Theme);
      await SetSyntaxHighlighting(parametrModel.UseSyntaxHighlighting);
      await SetCommandBodyBackgroundHighlighting(parametrModel.UseCommandBodyBackgroundHighlighting);
      await SetChainPointBodyBackgroundHighlighting(parametrModel.UseChainPointBodyBackgroundHighlighting);
      SaveUserInterfaceEvent?.Invoke(parametrModel);


      await LanguageSettings.SetLanguageAsync(UserInterfaceModel.Language);
      await ThemeSettings.SetThemeAsync(UserInterfaceModel.Theme);

      ThemeEventAdapter.RaiseSyntaxHighlighting(parametrModel.UseSyntaxHighlighting);
      ThemeEventAdapter.RaiseChangeTheme(parametrModel.Theme);
    }


    #endregion
  }
}
